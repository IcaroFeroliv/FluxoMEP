using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;

namespace AirConditioningClash.Utils
{
    public static class ModeladorTubulacao
    {
        public static void CriarDesvio90(Document doc, Pipe tuboOriginal, List<XYZ> rota)
        {
            // 1. Coletar dados do tubo original para clonar nas novas peças
            ElementId sistemaId = tuboOriginal.get_Parameter(BuiltInParameter.RBS_PIPING_SYSTEM_TYPE_PARAM).AsElementId();
            ElementId tipoTuboId = tuboOriginal.GetTypeId();
            ElementId nivelId = tuboOriginal.get_Parameter(BuiltInParameter.RBS_START_LEVEL_PARAM).AsElementId();
            double diametro = tuboOriginal.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).AsDouble();

            // Pontos da rota calculada
            XYZ ptCorteInicial = rota[0];
            XYZ ptSubida = rota[1];
            XYZ ptDescida = rota[2];
            XYZ ptCorteFinal = rota[3];

            // Ponto final original do tubo (para recriar o trecho final)
            LocationCurve locCurve = tuboOriginal.Location as LocationCurve;
            XYZ ptFinalOriginal = locCurve.Curve.GetEndPoint(1);

            // ---------------------------------------------------------
            // PASSO A: Modificar o Tubo Original (Encurtar até o corte)
            // ---------------------------------------------------------
            locCurve.Curve = Line.CreateBound(locCurve.Curve.GetEndPoint(0), ptCorteInicial);

            // ---------------------------------------------------------
            // PASSO B: Criar os Novos Segmentos de Tubo
            // ---------------------------------------------------------

            // 1. Tubo Vertical Subindo (CorteInicial -> Subida)
            Pipe tuboSubida = Pipe.Create(doc, sistemaId, tipoTuboId, nivelId, ptCorteInicial, ptSubida);

            // 2. Tubo Horizontal Ponte (Subida -> Descida)
            Pipe tuboPonte = Pipe.Create(doc, sistemaId, tipoTuboId, nivelId, ptSubida, ptDescida);

            // 3. Tubo Vertical Descendo (Descida -> CorteFinal)
            Pipe tuboDescida = Pipe.Create(doc, sistemaId, tipoTuboId, nivelId, ptDescida, ptCorteFinal);

            // 4. Tubo Final (Restante do original: CorteFinal -> FinalOriginal)
            Pipe tuboFinal = Pipe.Create(doc, sistemaId, tipoTuboId, nivelId, ptCorteFinal, ptFinalOriginal);

            // Ajustar diâmetros (Pipe.Create às vezes usa o padrão do tipo, forçamos o do original)
            AjustarDiametro(tuboSubida, diametro);
            AjustarDiametro(tuboPonte, diametro);
            AjustarDiametro(tuboDescida, diametro);
            AjustarDiametro(tuboFinal, diametro);

            // ---------------------------------------------------------
            // PASSO C: Conectar Tudo (Criar Cotovelos)
            // ---------------------------------------------------------

            // Regenera o documento para os conectores aparecerem nos lugares certos
            doc.Regenerate();

            // 1. Conectar Original com Subida
            ConectarTubos(doc, tuboOriginal, tuboSubida);

            // 2. Conectar Subida com Ponte
            ConectarTubos(doc, tuboSubida, tuboPonte);

            // 3. Conectar Ponte com Descida
            ConectarTubos(doc, tuboPonte, tuboDescida);

            // 4. Conectar Descida com Final
            ConectarTubos(doc, tuboDescida, tuboFinal);
        }

        private static void AjustarDiametro(Pipe tubo, double diametro)
        {
            Parameter p = tubo.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM);
            if (p != null && !p.IsReadOnly) p.Set(diametro);
        }

        private static void ConectarTubos(Document doc, Pipe tuboA, Pipe tuboB)
        {
            // Busca o conector do Tubo A que está mais perto do Tubo B
            Connector conectorA = GetConectorMaisProximo(tuboA, tuboB);
            Connector conectorB = GetConectorMaisProximo(tuboB, tuboA);

            if (conectorA != null && conectorB != null)
            {
                // Cria o Cotovelo (Elbow)
                doc.Create.NewElbowFitting(conectorA, conectorB);
            }
        }

        private static Connector GetConectorMaisProximo(Pipe tuboOrigem, Pipe tuboAlvo)
        {
            ConnectorManager cm = tuboOrigem.ConnectorManager;
            Connector melhorConector = null;
            double menorDistancia = double.MaxValue;

            // Pega o ponto central do tubo alvo para comparar distância
            LocationCurve locAlvo = tuboAlvo.Location as LocationCurve;
            XYZ centroAlvo = (locAlvo.Curve.GetEndPoint(0) + locAlvo.Curve.GetEndPoint(1)) / 2;

            foreach (Connector c in cm.Connectors)
            {
                double dist = c.Origin.DistanceTo(centroAlvo);
                if (dist < menorDistancia)
                {
                    menorDistancia = dist;
                    melhorConector = c;
                }
            }
            return melhorConector;
        }
    }
}