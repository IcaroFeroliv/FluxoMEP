using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using View = Autodesk.Revit.DB.View;

namespace AirConditioningClash.Utils
{
    public static class GeometriaUtils
    {
        /// <summary>
        /// Retorna a BoundingBox de um elemento vinculado convertida para as coordenadas do projeto atual (Host).
        /// </summary>
        public static BoundingBoxXYZ GetBoundingBoxNoMundo(RevitLinkInstance linkInst, Element elementoVinculado)
        {
            BoundingBoxXYZ boxOriginal = elementoVinculado.get_BoundingBox(null);
            if (boxOriginal == null) return null;

            Transform transform = linkInst.GetTotalTransform();
            XYZ minNoMundo = transform.OfPoint(boxOriginal.Min);
            XYZ maxNoMundo = transform.OfPoint(boxOriginal.Max);

            BoundingBoxXYZ boxMundo = new BoundingBoxXYZ();
            boxMundo.Min = new XYZ(
                Math.Min(minNoMundo.X, maxNoMundo.X),
                Math.Min(minNoMundo.Y, maxNoMundo.Y),
                Math.Min(minNoMundo.Z, maxNoMundo.Z));
            boxMundo.Max = new XYZ(
                Math.Max(minNoMundo.X, maxNoMundo.X),
                Math.Max(minNoMundo.Y, maxNoMundo.Y),
                Math.Max(minNoMundo.Z, maxNoMundo.Z));

            return boxMundo;
        }

        // ─────────────────────────────────────────────────────────────────────
        // VISIBILIDADE EM CORTE
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Retorna o ponto de inserção ou centro físico do elemento.</summary>
        public static XYZ GetElementCenter(Element elem)
        {
            if (elem.Location is LocationPoint lp) return lp.Point;
            if (elem.Location is LocationCurve lc) return lc.Curve.Evaluate(0.5, true);
            BoundingBoxXYZ bbox = elem.get_BoundingBox(null);
            if (bbox != null) return (bbox.Min + bbox.Max) / 2.0;
            return null;
        }

        /// <summary>
        /// Verifica se o CENTRO do elemento está dentro do volume de corte (CropBox)
        /// convertendo para o espaço local da vista.
        /// Aplicável apenas para ViewSection (corte/elevação).
        /// </summary>
        public static bool IsElementPhysicallyInSection(View sectionView, Element elem)
        {
            XYZ center = GetElementCenter(elem);
            if (center == null) return false;

            Transform worldToLocal = sectionView.CropBox.Transform.Inverse;
            XYZ localPos = worldToLocal.OfPoint(center);

            const double tol = 0.5; // ~15 cm de margem nas bordas

            if (localPos.X < sectionView.CropBox.Min.X - tol || localPos.X > sectionView.CropBox.Max.X + tol ||
                localPos.Y < sectionView.CropBox.Min.Y - tol || localPos.Y > sectionView.CropBox.Max.Y + tol)
                return false;

            Parameter farClipParam = sectionView.get_Parameter(BuiltInParameter.VIEWER_BOUND_FAR_CLIPPING);
            bool farClipAtivo = farClipParam != null && farClipParam.AsInteger() != 0;

            if (farClipAtivo)
            {
                double minZ = sectionView.CropBox.Min.Z;
                double maxZ = sectionView.CropBox.Max.Z;
                if (localPos.Z < minZ - tol || localPos.Z > maxZ + tol)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Verifica se o elemento está TOTALMENTE oculto por uma parede, laje ou outro
        /// equipamento no corte — comparando bounding boxes no espaço DO CORTE.
        ///
        /// Eixos usados:
        ///   • Profundidade  = projeção em ViewDirection  (positivo = para dentro da cena)
        ///   • Horizontal    = projeção em RightDirection
        ///   • Vertical      = projeção em UpDirection
        ///
        /// Por que usar ViewDirection e não CropBox.Transform.Inverse para a profundidade:
        ///   O CropBox.BasisZ aponta EM DIREÇÃO AO OBSERVADOR (oposto ao ViewDirection)
        ///   em muitas orientações de corte. Isso fazia os elementos visíveis terem Z
        ///   negativo, tornando as comparações de profundidade completamente erradas.
        ///   ViewDirection é SEMPRE correto: positivo = visível na cena.
        ///
        /// Lógica de detecção:
        ///   Para cada bloqueador (parede/laje do HOST e de Revit Links):
        ///     1. Profundidade do bloqueador < profundidade do elemento − 200 mm
        ///        → bloqueador está na frente do elemento
        ///     2. Sobreposição XY ≥ 60% da projeção do elemento
        ///        → bloqueador cobre o elemento
        ///   Se ambas as condições são verdadeiras → elemento está OCULTO.
        /// </summary>
        public static bool IsOccludedByArchitecture(Document doc, View sectionView, Element elem)
        {
            // Direções do corte no espaço mundo — sempre confiáveis
            XYZ viewDir  = sectionView.ViewDirection;   // positivo = para dentro da cena
            XYZ rightDir = sectionView.RightDirection;
            XYZ upDir    = sectionView.UpDirection;
            XYZ origin   = sectionView.Origin;          // ponto no plano de corte

            // --- Profundidade do CENTRO para o check de "está na cena" ---
            XYZ elemCenter = GetElementCenter(elem);
            if (elemCenter == null) return false;
            if ((elemCenter - origin).DotProduct(viewDir) < 0.05) return false;

            // --- BBox do elemento projetado no espaço do corte ---
            BoundingBoxXYZ elemBBox = elem.get_BoundingBox(null);
            if (elemBBox == null) return false;

            double eXMin, eXMax, eYMin, eYMax, eDMin, eDMax;
            GetExtentsEmCorte(elemBBox, Transform.Identity, origin, rightDir, upDir, viewDir,
                out eXMin, out eXMax, out eYMin, out eYMax, out eDMin, out eDMax);

            // eDMin = profundidade da FACE FRONTAL do elemento (mais perto do observador).
            // Usamos eDMin, não o centro, para comparação com bloqueadores — isso resolve
            // o caso de equipamentos wall-hosted cujo LocationPoint fica NA face da parede:
            // a parede que bloqueia a visão tem bDMin < eDMin, detectada corretamente.
            if (eDMin < 0.05) return false; // Face frontal no plano de corte → visível

            double eWidth  = eXMax - eXMin;
            double eHeight = eYMax - eYMin;
            if (eWidth < 0.001 || eHeight < 0.001) return false;

            // Reduz 15% nas bordas — ignora sobreposição superficial com elementos adjacentes
            const double shrink = 0.15;
            eXMin += eWidth * shrink;   eXMax -= eWidth * shrink;
            eYMin += eHeight * shrink;  eYMax -= eHeight * shrink;

            double elemArea = (eXMax - eXMin) * (eYMax - eYMin);
            if (elemArea < 1e-6) return false;

            // Limiar de cobertura: ≥ 60% da projeção XY coberta por UM bloqueador → oculto
            const double limiarCobertura = 0.60;

            // Epsilon de profundidade: o bloqueador precisa estar pelo menos 10 mm
            // NA FRENTE da face frontal do elemento (não "junto" ou "atrás").
            // Não há mais "hospedeirTol": a lógica usa a face frontal (eDMin) diretamente.
            const double epsilonDepth = 0.033; // ~10 mm em pés internos do Revit

            // --- Categorias que bloqueiam visualmente ---
            var catFilter = new ElementMulticategoryFilter(new List<BuiltInCategory>
            {
                BuiltInCategory.OST_Walls,
                BuiltInCategory.OST_Floors,
                BuiltInCategory.OST_Ceilings,
                BuiltInCategory.OST_Roofs,
                BuiltInCategory.OST_MechanicalEquipment,
            });

            // --- Bloqueadores do documento HOST visíveis nesta vista ---
            var hostBlockers = new FilteredElementCollector(doc, sectionView.Id)
                .WherePasses(catFilter)
                .WhereElementIsNotElementType()
                .Where(e => e.Id != elem.Id)
                .ToList();

            if (VerificarCobertura(
                    hostBlockers, Transform.Identity,
                    origin, rightDir, upDir, viewDir,
                    eDMin, epsilonDepth,
                    eXMin, eXMax, eYMin, eYMax, elemArea, limiarCobertura))
                return true;

            // --- Bloqueadores em Revit Links (arquitetura, estrutura) ---
            var linkCatFilter = new ElementMulticategoryFilter(new List<BuiltInCategory>
            {
                BuiltInCategory.OST_Walls,
                BuiltInCategory.OST_Floors,
                BuiltInCategory.OST_Ceilings,
                BuiltInCategory.OST_Roofs,
            });

            foreach (RevitLinkInstance linkInst in new FilteredElementCollector(doc)
                     .OfClass(typeof(RevitLinkInstance)).Cast<RevitLinkInstance>())
            {
                Document linkDoc = linkInst.GetLinkDocument();
                if (linkDoc == null) continue;

                var linkBlockers = new FilteredElementCollector(linkDoc)
                    .WherePasses(linkCatFilter)
                    .WhereElementIsNotElementType()
                    .ToList();

                if (VerificarCobertura(
                        linkBlockers, linkInst.GetTotalTransform(),
                        origin, rightDir, upDir, viewDir,
                        eDMin, epsilonDepth,
                        eXMin, eXMax, eYMin, eYMax, elemArea, limiarCobertura))
                    return true;
            }

            return false;
        }

        // ─────────────────────────────────────────────────────────────────────
        // HELPERS PRIVADOS
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Para cada bloqueador, verifica se está na frente do elemento E cobre ≥ limiarCobertura
        /// da projeção XY do elemento. Retorna true na primeira detecção (early exit).
        /// </summary>
        private static bool VerificarCobertura(
            IEnumerable<Element> blockers,
            Transform toWorld,          // Transform que leva o bbox do bloqueador ao espaço mundo
            XYZ origin, XYZ rightDir, XYZ upDir, XYZ viewDir,
            double elemDepth,
            double hospedeirTol,
            double eXMin, double eXMax,
            double eYMin, double eYMax,
            double elemArea,
            double limiarCobertura)
        {
            foreach (Element blocker in blockers)
            {
                BoundingBoxXYZ bBox = blocker.get_BoundingBox(null);
                if (bBox == null) continue;

                double bXMin, bXMax, bYMin, bYMax, bDMin, bDMax;
                GetExtentsEmCorte(bBox, toWorld, origin, rightDir, upDir, viewDir,
                    out bXMin, out bXMax, out bYMin, out bYMax, out bDMin, out bDMax);

                // O bloqueador precisa:
                //   • Ter parte no lado visível do corte (profundidade > 0.05)
                //   • Estar na frente do elemento com margem hospedeira
                if (bDMax < 0.05) continue;                          // inteiramente atrás do plano de corte
                if (bDMin >= elemDepth - hospedeirTol) continue;      // atrás ou junto ao elemento

                // Sobreposição XY entre bloqueador e elemento
                double overlapX = Math.Min(bXMax, eXMax) - Math.Max(bXMin, eXMin);
                double overlapY = Math.Min(bYMax, eYMax) - Math.Max(bYMin, eYMin);

                if (overlapX <= 0 || overlapY <= 0) continue;

                double cobertura = (overlapX * overlapY) / elemArea;
                if (cobertura >= limiarCobertura)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Projeta os 8 cantos de uma BoundingBox nas direções do corte (Right, Up, View)
        /// usando produtos escalares em relação ao origin do plano de corte.
        ///
        /// toWorld: transforma o bbox do espaço local (link ou host) para o espaço mundo.
        ///   • Elementos do HOST: passar Transform.Identity (bbox já está no mundo)
        ///   • Elementos de LINK: passar linkInstance.GetTotalTransform()
        /// </summary>
        private static void GetExtentsEmCorte(
            BoundingBoxXYZ bbox,
            Transform toWorld,
            XYZ origin, XYZ rightDir, XYZ upDir, XYZ viewDir,
            out double xMin, out double xMax,
            out double yMin, out double yMax,
            out double depthMin, out double depthMax)
        {
            XYZ mn = bbox.Min, mx = bbox.Max;

            // Os 8 cantos no espaço local (host ou link)
            XYZ[] corners =
            {
                new XYZ(mn.X, mn.Y, mn.Z), new XYZ(mx.X, mn.Y, mn.Z),
                new XYZ(mn.X, mx.Y, mn.Z), new XYZ(mx.X, mx.Y, mn.Z),
                new XYZ(mn.X, mn.Y, mx.Z), new XYZ(mx.X, mn.Y, mx.Z),
                new XYZ(mn.X, mx.Y, mx.Z), new XYZ(mx.X, mx.Y, mx.Z),
            };

            xMin = double.MaxValue;  xMax = double.MinValue;
            yMin = double.MaxValue;  yMax = double.MinValue;
            depthMin = double.MaxValue; depthMax = double.MinValue;

            foreach (XYZ c in corners)
            {
                // Transforma para o espaço mundo
                XYZ w = toWorld.OfPoint(c);
                XYZ rel = w - origin;

                double x = rel.DotProduct(rightDir);
                double y = rel.DotProduct(upDir);
                double d = rel.DotProduct(viewDir);

                if (x < xMin) xMin = x;
                if (x > xMax) xMax = x;
                if (y < yMin) yMin = y;
                if (y > yMax) yMax = y;
                if (d < depthMin) depthMin = d;
                if (d > depthMax) depthMax = d;
            }
        }
    }
}
