using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace AirConditioningClash.Utils
{
    public static class CalculadoraDesvio
    {
        // Agora recebemos 'vetorDesvioTotal' em vez de altura
        public static List<XYZ> CalcularRota90Graus(
            XYZ p1Tubo,
            XYZ p2Tubo,
            BoundingBoxXYZ boxViga,
            double alturaAtualTubo,
            XYZ vetorDesvioTotal, // O vetor que aponta para onde o tubo vai se mover (distancia inclusa)
            double margemLateral)
        {
            List<XYZ> rota = new List<XYZ>();

            XYZ direcaoTubo = (p2Tubo - p1Tubo).Normalize();

            // 1. Pegar geometria da viga (igual antes)
            List<XYZ> cantosViga = new List<XYZ>
            {
                new XYZ(boxViga.Min.X, boxViga.Min.Y, alturaAtualTubo),
                new XYZ(boxViga.Max.X, boxViga.Min.Y, alturaAtualTubo),
                new XYZ(boxViga.Max.X, boxViga.Max.Y, alturaAtualTubo),
                new XYZ(boxViga.Min.X, boxViga.Max.Y, alturaAtualTubo)
            };

            // 2. Projeção para achar inicio e fim (igual antes)
            double minProj = double.MaxValue;
            double maxProj = double.MinValue;

            foreach (XYZ canto in cantosViga)
            {
                XYZ vetorParaCanto = canto - p1Tubo;
                double projecao = vetorParaCanto.DotProduct(direcaoTubo);
                if (projecao < minProj) minProj = projecao;
                if (projecao > maxProj) maxProj = projecao;
            }

            XYZ pontoInicioObstaculo = p1Tubo + (direcaoTubo * minProj);
            XYZ pontoFimObstaculo = p1Tubo + (direcaoTubo * maxProj);

            // 3. Pontos base na linha original (Antes e Depois)
            XYZ pontoAntes = pontoInicioObstaculo - (direcaoTubo * margemLateral);
            XYZ pontoDepois = pontoFimObstaculo + (direcaoTubo * margemLateral);

            // 4. Pontos Desviados (AQUI É A MUDANÇA)
            // Simplesmente somamos o vetor calculado ao ponto base.
            // Isso funciona para Cima, Baixo, Esquerda, Direita, Diagonal...
            XYZ pontoSubida = pontoAntes + vetorDesvioTotal;
            XYZ pontoDescida = pontoDepois + vetorDesvioTotal;

            rota.Add(pontoAntes);
            rota.Add(pontoSubida);
            rota.Add(pontoDescida);
            rota.Add(pontoDepois);

            return rota;
        }
    }
}