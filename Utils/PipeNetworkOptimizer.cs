using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;

namespace AirConditioningClash.Utils
{
    public static class PipeNetworkOptimizer
    {
        // Configuração: Ignorar tubos menores que 40cm (0.4m ~ 1.3 pés)
        // Isso evita taggear "tocos" de conexão.
        private const double MIN_PIPE_LENGTH_FEET = 1.3;

        public static List<Pipe> GetOptimizedPipesToTag(Document doc, List<Pipe> visiblePipesInView)
        {
            List<Pipe> pipesToTag = new List<Pipe>();
            HashSet<ElementId> visitedElementIds = new HashSet<ElementId>();

            // Para acesso rápido, criamos um HashSet apenas com os IDs dos tubos VISÍVEIS
            // Isso serve para, no final, garantir que o "campeão" escolhido seja um tubo visível.
            HashSet<ElementId> visibleIds = new HashSet<ElementId>(visiblePipesInView.Select(p => p.Id));

            foreach (var pipe in visiblePipesInView)
            {
                if (visitedElementIds.Contains(pipe.Id)) continue;

                // 1. Rastreia a rede COMPLETA (mesmo passando por itens ocultos)
                List<Pipe> networkPipes = new List<Pipe>();
                FindConnectedNetwork(pipe, visitedElementIds, networkPipes);

                // 2. Escolhe o representante
                if (networkPipes.Count > 0)
                {
                    // Filtra: Só queremos eleger tubos que sejam VISÍVEIS na vista atual
                    var visibleCandidates = networkPipes.Where(p => visibleIds.Contains(p.Id)).ToList();

                    if (visibleCandidates.Count > 0)
                    {
                        // LÓGICA DE OURO:
                        // 1. Tenta pegar tubos maiores que o mínimo (evita nipples)
                        var validCandidates = visibleCandidates.Where(p => p.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH).AsDouble() > MIN_PIPE_LENGTH_FEET).ToList();

                        // Se não sobrou nenhum (só tem toco), usa os tocos mesmo. Se sobrou, usa os grandes.
                        var finalCandidates = (validCandidates.Count > 0) ? validCandidates : visibleCandidates;

                        // Eleição: Maior Comprimento > Horizontal
                        Pipe bestPipe = finalCandidates
                            .OrderByDescending(p => p.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH).AsDouble())
                            .ThenByDescending(p => IsHorizontal(p))
                            .First();

                        pipesToTag.Add(bestPipe);
                    }
                }
            }

            return pipesToTag;
        }

        private static void FindConnectedNetwork(Pipe currentPipe, HashSet<ElementId> visited, List<Pipe> network)
        {
            // Fila para navegação (BFS)
            Queue<Element> queue = new Queue<Element>();
            queue.Enqueue(currentPipe);
            visited.Add(currentPipe.Id);
            network.Add(currentPipe);

            // Proteção contra loops infinitos (máximo de elementos numa única rede)
            int safetyCounter = 0;

            while (queue.Count > 0 && safetyCounter < 500)
            {
                safetyCounter++;
                Element current = queue.Dequeue();

                // Pega conectores (seja tubo ou conexão)
                ConnectorManager cm = GetConnectorManager(current);
                if (cm == null) continue;

                foreach (Connector conn in cm.Connectors)
                {
                    if (!conn.IsConnected) continue;

                    foreach (Connector refConn in conn.AllRefs)
                    {
                        // O vizinho (pode ser tubo, joelho, tê, equipamento...)
                        Element neighbor = refConn.Owner;

                        // Ignora se for o próprio elemento ou nulo
                        if (neighbor == null || neighbor.Id == current.Id) continue;

                        // Ignora se já visitamos
                        if (visited.Contains(neighbor.Id)) continue;

                        // Verifica categorias válidas para propagação (Tubos e Conexões apenas)
                        BuiltInCategory catId = (BuiltInCategory)neighbor.Category.Id.Value;
                        bool isPipe = catId == BuiltInCategory.OST_PipeCurves;
                        bool isFitting = catId == BuiltInCategory.OST_PipeFitting;

                        if (isPipe || isFitting)
                        {
                            visited.Add(neighbor.Id);
                            queue.Enqueue(neighbor);

                            // Se for tubo, adiciona na lista da rede para possível eleição
                            if (isPipe && neighbor is Pipe p)
                            {
                                network.Add(p);
                            }
                        }
                    }
                }
            }
        }

        private static ConnectorManager GetConnectorManager(Element e)
        {
            if (e is Pipe p) return p.ConnectorManager;
            if (e is FamilyInstance fi) return fi.MEPModel?.ConnectorManager;
            return null;
        }

        private static bool IsHorizontal(Pipe p)
        {
            LocationCurve lc = p.Location as LocationCurve;
            if (lc == null) return false;
            Line l = lc.Curve as Line;
            return Math.Abs(l.Direction.Z) < 0.1;
        }
    }
}