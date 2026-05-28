using AirConditioningClash.Utils;
using AirConditioningClash.Views;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using View = Autodesk.Revit.DB.View;

namespace AirConditioningClash.Commands.Climatizacao
{
    [Transaction(TransactionMode.Manual)]
    public class ComandoDetalhamento : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            try
            {
                // 1. UI Configuration
                Views.Climatizacao.TagConfigView view = new Views.Climatizacao.TagConfigView(doc);
                if (view.ShowDialog() != true) return Result.Cancelled;
                TagSettings settings = view.Settings;

                // 2. Busca Familia
                FamilySymbol tagSymbol = new FilteredElementCollector(doc)
                    .OfClass(typeof(FamilySymbol)).Cast<FamilySymbol>()
                    .FirstOrDefault(x => x.Name == settings.LastFamilySymbolName);

                if (tagSymbol == null)
                {
                    TaskDialog.Show("Erro", "Família de tag não encontrada.");
                    return Result.Failed;
                }

                // 3. SELEÇÃO E FILTRAGEM
                List<Element> elementsToTag = new List<Element>();
                View activeView = doc.ActiveView;
                FilteredElementCollector viewCollector = new FilteredElementCollector(doc, activeView.Id);

                bool isCorte = activeView is ViewSection &&
                               (activeView.ViewType == ViewType.Section || activeView.ViewType == ViewType.Elevation);

                // Converte o comprimento mínimo da UI para unidades internas do Revit (pés)
                double minLengthInternal = UnitUtils.ConvertToInternalUnits(settings.MinimumLengthMm, UnitTypeId.Millimeters);

                if (settings.IsPipeCategory || settings.IsDuctCategory)
                {
                    // Define se vamos buscar Pipe (Tubo) ou Duct (Duto)
                    Type typeToFilter = settings.IsPipeCategory ? typeof(Pipe) : typeof(Duct);

                    var potentialCurves = viewCollector.OfClass(typeToFilter).Cast<MEPCurve>().ToList();

                    foreach (var curve in potentialCurves)
                    {
                        if (curve.IsHidden(activeView)) continue;

                        // Em cortes: verifica se o elemento está fisicamente no volume do corte
                        // E se não está ocluído por paredes ou outros elementos.
                        if (isCorte)
                        {
                            if (!GeometriaUtils.IsElementPhysicallyInSection(activeView, curve)) continue;
                            if (GeometriaUtils.IsOccludedByArchitecture(doc, activeView, curve)) continue;
                        }
                        else
                        {
                            if (!IsElementVisibleInView(activeView, curve)) continue;
                        }

                        Parameter lenParam = curve.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH);
                        if (lenParam != null && lenParam.AsDouble() < minLengthInternal) continue;

                        elementsToTag.Add(curve);
                    }

                    if (elementsToTag.Count == 0)
                    {
                        TaskDialog.Show("Aviso", "Nenhum item que atenda aos critérios (visibilidade/comprimento) foi encontrado.");
                        return Result.Cancelled;
                    }
                }
                else
                {
                    // Lógica para Equipamentos (não possuem comprimento, então o filtro não se aplica)
                    List<Element> potentialEquip = viewCollector
                        .OfCategory(BuiltInCategory.OST_MechanicalEquipment)
                        .WhereElementIsNotElementType()
                        .ToList();

                    foreach (var equip in potentialEquip)
                    {
                        if (equip.IsHidden(activeView)) continue;

                        // Em cortes: verifica posição física no volume e oclusão por obstáculos.
                        if (isCorte)
                        {
                            if (!GeometriaUtils.IsElementPhysicallyInSection(activeView, equip)) continue;
                            if (GeometriaUtils.IsOccludedByArchitecture(doc, activeView, equip)) continue;
                        }
                        else
                        {
                            if (!IsElementVisibleInView(activeView, equip)) continue;
                        }

                        elementsToTag.Add(equip);
                    }
                }

                // 4. EXECUÇÃO
                using (Transaction t = new Transaction(doc, "Inserir Tags HVAC"))
                {
                    t.Start();

                    // Valores para Tubos/Equipamentos
                    double offY = UnitUtils.ConvertToInternalUnits(settings.OffsetForHorizontalPipesMm, UnitTypeId.Millimeters);
                    double offX = UnitUtils.ConvertToInternalUnits(settings.OffsetForVerticalPipesMm, UnitTypeId.Millimeters);
                    double stack = UnitUtils.ConvertToInternalUnits(settings.StackDistanceMm, UnitTypeId.Millimeters);
                    double collisionTolerance = UnitUtils.ConvertToInternalUnits(300, UnitTypeId.Millimeters);

                    if (settings.IsDuctCategory)
                    {
                        // LÓGICA SIMPLIFICADA PARA DUTOS
                        // Como você quer apenas o local padrão, usamos o ponto médio sem offsets
                        foreach (Element duct in elementsToTag)
                        {
                            LocationCurve lc = duct.Location as LocationCurve;
                            if (lc == null) continue;

                            XYZ midPoint = lc.Curve.Evaluate(0.5, true); // Ponto central do duto

                            // Cria a tag no ponto central. 
                            // 'false' no final indica que não precisa de cálculos de "elbow" (cotovelo)
                            CreateTagWithElbow(doc, duct, tagSymbol, midPoint, settings.HasLeader, true);
                        }
                    }
                    else if (settings.IsPipeCategory)
                    {
                        // Mantém sua lógica complexa original apenas para tubos
                        ProcessPipesWithSpatialStacking(doc, elementsToTag.Cast<Pipe>().ToList(), settings, tagSymbol, offX, offY, stack, collisionTolerance);
                    }
                    else
                    {
                        // Equipamentos
                        ProcessEquipment(doc, elementsToTag, settings, tagSymbol, offX, offY);
                    }

                    t.Commit();
                }

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = "Erro: " + ex.Message;
                return Result.Failed;
            }
        }

        private void ProcessPipesWithSpatialStacking(Document doc, List<Pipe> pipes, TagSettings settings, FamilySymbol symbol, double offX, double offY, double stackDist, double alignTolerance)
        {
            List<Pipe> hPipes = new List<Pipe>();
            List<Pipe> vPipes = new List<Pipe>();

            // Separação inicial
            foreach (var p in pipes)
            {
                LocationCurve lc = p.Location as LocationCurve;
                if (lc == null) continue;
                Line l = lc.Curve as Line;
                if (Math.Abs(l.Direction.X) > Math.Abs(l.Direction.Y)) hPipes.Add(p); else vPipes.Add(p);
            }

            // --- PROCESSAMENTO TUBOS HORIZONTAIS ---
            if (hPipes.Count > 0)
            {
                // Ordenação CRÍTICA para o empilhamento funcionar:
                // Se a tag vai para CIMA (Top), ordenamos os tubos de BAIXO para CIMA (Y Crescente).
                // Assim, colocamos a tag do tubo mais baixo, depois a do tubo de cima "bate" nele e sobe.
                if (settings.HorizontalPipePlacement == TagPlacement.Top)
                    hPipes = hPipes.OrderBy(p => GetMidPoint(p).Y).ToList();
                else
                    hPipes = hPipes.OrderByDescending(p => GetMidPoint(p).Y).ToList();

                // Lista de locais onde JÁ COLOCAMOS tags (para evitar colisão)
                List<XYZ> placedTagLocations = new List<XYZ>();

                foreach (var pipe in hPipes)
                {
                    // 1. Calcula Posição Ideal
                    XYZ pipeMid = GetMidPoint(pipe);
                    XYZ direction = settings.HorizontalPipePlacement == TagPlacement.Top ? XYZ.BasisY : -XYZ.BasisY;
                    XYZ idealPos = pipeMid + direction * offY;

                    // 2. Verifica Colisão e Ajusta (Soma espaçamento)
                    XYZ finalPos = ResolveCollision(idealPos, placedTagLocations, true, stackDist, alignTolerance, direction);

                    // 3. Cria e Registra
                    CreateTagWithElbow(doc, pipe, symbol, finalPos, settings.HasLeader, true);
                    placedTagLocations.Add(finalPos);
                }
            }

            // --- PROCESSAMENTO TUBOS VERTICAIS ---
            if (vPipes.Count > 0)
            {
                // Se a tag vai para DIREITA, ordenamos ESQUERDA -> DIREITA (X Crescente)
                if (settings.VerticalPipePlacement == TagPlacement.Right)
                    vPipes = vPipes.OrderBy(p => GetMidPoint(p).X).ToList();
                else
                    vPipes = vPipes.OrderByDescending(p => GetMidPoint(p).X).ToList();

                List<XYZ> placedTagLocations = new List<XYZ>();

                foreach (var pipe in vPipes)
                {
                    XYZ pipeMid = GetMidPoint(pipe);
                    // Correção: Tubos Verticais empilham em Y (para formar lista), mas deslocam em X
                    XYZ offsetDir = settings.VerticalPipePlacement == TagPlacement.Right ? XYZ.BasisX : -XYZ.BasisX;

                    // A direção do EMPILHAMENTO (stack) para verticais é Y (BasisY)
                    XYZ stackDir = XYZ.BasisY;

                    XYZ idealPos = pipeMid + offsetDir * offX;

                    // Para verticais, precisamos passar o stackDir explicitamente
                    XYZ finalPos = ResolveCollision(idealPos, placedTagLocations, false, stackDist, alignTolerance, stackDir);

                    // Alinhamento Fino: Mantém o X alinhado com o Offset, só muda o Y
                    finalPos = new XYZ(idealPos.X, finalPos.Y, finalPos.Z);

                    CreateTagWithElbow(doc, pipe, symbol, finalPos, settings.HasLeader, false);
                    placedTagLocations.Add(finalPos);
                }
            }
        }

        // --- O SEGREDO: DETECTOR DE COLISÃO ---
        private XYZ ResolveCollision(XYZ proposed, List<XYZ> existing, bool isHorizontalPipe, double stackDist, double tolerance, XYZ stackDirection)
        {
            XYZ currentPos = proposed;
            int safety = 0;
            bool collisionFound = true;

            while (collisionFound && safety < 10) // Tenta empilhar até 10 vezes
            {
                collisionFound = false;
                foreach (XYZ placed in existing)
                {
                    // Verifica se a tag 'placed' está na mesma "coluna" visual (dentro da tolerancia)
                    // Se for tubo horizontal, verifica se o X está perto.
                    // Se for tubo vertical, verifica se o X está perto (pois empilham verticalmente).

                    double distParallel = isHorizontalPipe
                        ? Math.Abs(placed.X - currentPos.X)  // Tubo deitado: Tags alinhadas no X?
                        : Math.Abs(placed.X - currentPos.X); // Tubo em pé: Tags alinhadas no X? (Lista vertical)

                    if (distParallel < tolerance)
                    {
                        // Estão alinhadas. Agora verifica se estão sobrepostas no sentido do empilhamento
                        double distStack = isHorizontalPipe
                            ? Math.Abs(placed.Y - currentPos.Y)
                            : Math.Abs(placed.Y - currentPos.Y);

                        // Se a distância for menor que o espaçamento desejado, temos COLISÃO!
                        // (Usamos stackDist * 0.9 para dar uma margem)
                        if (distStack < stackDist * 0.9)
                        {
                            collisionFound = true;
                            // BATEU! Soma o espaçamento e tenta de novo
                            currentPos = currentPos + stackDirection * stackDist;
                            break; // Reinicia a verificação com a nova posição
                        }
                    }
                }
                safety++;
            }
            return currentPos;
        }

        private void ProcessEquipment(Document doc, List<Element> equipments, TagSettings settings, FamilySymbol symbol, double offX, double offY)
        {
            foreach (var equip in equipments)
            {
                BoundingBoxXYZ bbox = equip.get_BoundingBox(doc.ActiveView);
                if (bbox == null) continue;

                XYZ center = (bbox.Max + bbox.Min) / 2;

                // --- RECALIBRAÇÃO TOTAL DO EIXO Y (PARA PLANTA DE FORRO) ---
                double finalY;

                // Pegamos a altura total da máquina (distância entre o topo e o fundo)
                double height = bbox.Max.Y - bbox.Min.Y;

                if (settings.HorizontalPipePlacement == TagPlacement.Top)
                {
                    // ALTO: Pega a borda superior E SOMA o seu offset. 
                    // Se o alto ainda parece baixo, vamos garantir que ele seja NO MÍNIMO 
                    // o topo da máquina + a altura dela + o offset.
                    finalY = bbox.Max.Y + offY;
                }
                else if (settings.HorizontalPipePlacement == TagPlacement.Center)
                {
                    // CENTRO: O que você quer que seja o "Alto" (sentado na borda de cima)
                    finalY = bbox.Max.Y;
                }
                else // BAIXO
                {
                    // BAIXO: Agora será o Centro real, para não ficar enterrado
                    finalY = center.Y;
                }

                // --- RECALIBRAÇÃO DO EIXO X (VERTICAL NA UI) ---
                double finalX;
                if (settings.VerticalPipePlacement == TagPlacement.Right)
                    finalX = bbox.Max.X + offX;
                else if (settings.VerticalPipePlacement == TagPlacement.Left)
                    finalX = bbox.Min.X - offX;
                else
                    finalX = center.X;

                XYZ headPos = new XYZ(finalX, finalY, center.Z);

                // --- CRIAÇÃO COM FORÇA BRUTA ---
                IndependentTag tag = IndependentTag.Create(doc, symbol.Id, doc.ActiveView.Id, new Reference(equip), false, TagOrientation.Horizontal, headPos);

                // Forçamos a posição da cabeça (Head)
                tag.TagHeadPosition = headPos;

                if (settings.HasLeader)
                {
                    tag.HasLeader = true;
                    try
                    {
                        // Se estiver no ALTO, a seta toca o topo da máquina para a linha não ficar gigante
                        XYZ targetPoint = settings.HorizontalPipePlacement == TagPlacement.Top ? new XYZ(center.X, bbox.Max.Y, center.Z) : center;

                        tag.SetLeaderEnd(new Reference(equip), targetPoint);
                        tag.TagHeadPosition = headPos;

                        if (finalX != center.X || finalY != center.Y)
                        {
                            XYZ elbow = new XYZ(targetPoint.X, headPos.Y, headPos.Z);
                            tag.SetLeaderElbow(new Reference(equip), elbow);
                        }
                    }
                    catch { }
                }
            }
        }
        

        private void CreateTagWithElbow(Document doc, Element elem, FamilySymbol symbol, XYZ headPos, bool leader, bool horizontalRun)
        {
            Reference refElem = new Reference(elem);

            // 1. Cria a tag em uma posição temporária
            IndependentTag tag = IndependentTag.Create(doc, symbol.Id, doc.ActiveView.Id, refElem, leader, TagOrientation.Horizontal, headPos);

            // 2. Tenta forçar a posição da cabeça
            tag.TagHeadPosition = headPos;

            if (leader)
            {
                XYZ target = XYZ.Zero;
                if (elem.Location is LocationCurve lc)
                    target = lc.Curve.Project(headPos).XYZPoint;
                else if (elem.Location is LocationPoint lp)
                    target = lp.Point;

                // Forçamos o target a ter o mesmo Z do headPos para evitar distorções
                target = new XYZ(target.X, target.Y, headPos.Z);

                XYZ elbow = headPos;
                if (horizontalRun)
                    elbow = new XYZ(target.X, headPos.Y, headPos.Z);
                else
                    elbow = new XYZ(headPos.X, target.Y, headPos.Z);

                try
                {
                    // Define as extremidades
                    tag.SetLeaderEnd(refElem, target);
                    tag.TagHeadPosition = headPos; // Repete para garantir
                    tag.SetLeaderElbow(refElem, elbow);
                }
                catch { }
            }
        }

        private XYZ GetMidPoint(Pipe p)
        {
            LocationCurve lc = p.Location as LocationCurve;
            if (lc == null) return XYZ.Zero;
            return lc.Curve.Evaluate(0.5, true);
        }

        private bool IsElementVisibleInView(View view, Element element)
        {
            if (view == null || element == null) return false;
            try
            {
                if (element.IsHidden(view)) return false;
                Options opt = new Options();
                opt.View = view;
                GeometryElement geo = element.get_Geometry(opt);
                if (geo == null) return false;
                foreach (var obj in geo)
                {
                    if (obj is Solid s && s.Volume > 0.0001) return true;
                    if (obj is Line) return true;
                    if (obj is GeometryInstance) return true;
                }
                return false;
            }
            catch { return false; }
        }
    }
}