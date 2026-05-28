using System.Collections.Generic;
using System.Linq;

namespace AirConditioningClash.Models.Eletrica
{
    public class ProcessamentoConduitesResult
    {
        public List<ProcessamentoConduiteItemResult> Itens { get; set; } = new List<ProcessamentoConduiteItemResult>();

        public int Total
        {
            get { return Itens.Count; }
        }

        public int Sucessos
        {
            get { return Itens.Count(x => x.Status == EnumStatusProcessamentoConduite.Sucesso); }
        }

        public int IgnoradosSemParametros
        {
            get { return Itens.Count(x => x.Status == EnumStatusProcessamentoConduite.IgnoradoSemParametros); }
        }

        public int IgnoradosSemCabosMapeados
        {
            get { return Itens.Count(x => x.Status == EnumStatusProcessamentoConduite.IgnoradoSemCabosMapeados); }
        }

        public int ErrosLeitura
        {
            get { return Itens.Count(x => x.Status == EnumStatusProcessamentoConduite.ErroLeitura); }
        }

        public int ErrosCalculo
        {
            get { return Itens.Count(x => x.Status == EnumStatusProcessamentoConduite.ErroCalculo); }
        }

        public int ErrosAplicacao
        {
            get { return Itens.Count(x => x.Status == EnumStatusProcessamentoConduite.ErroAplicacao); }
        }

        public List<ProcessamentoConduiteItemResult> ListaSemParametros()
        {
            return Itens
                .Where(x => x.Status == EnumStatusProcessamentoConduite.IgnoradoSemParametros)
                .ToList();
        }

        public List<ProcessamentoConduiteItemResult> ListaErrosAplicacao()
        {
            return Itens
                .Where(x => x.Status == EnumStatusProcessamentoConduite.ErroAplicacao)
                .ToList();
        }
    }
}