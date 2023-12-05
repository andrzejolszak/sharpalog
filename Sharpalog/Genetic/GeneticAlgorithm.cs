namespace Sharplog.Genetic
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Sharplog;
    using Sharplog.Engine;

    public class GeneticAlgorithm
    {
        private List<Universe> _population;
        private BottomUpEngine _engine = new BottomUpEngine();

        public void InitializePopulation(int count, Universe prototype)
        {
            this._population = new List<Universe>();
            for(int i = 0; i < count; i++)
            {
                Universe clone = this.Clone(prototype);
                this.Randomize(clone);
                this._population.Add(clone);
            }
        }

        public void Randomize(Universe clone)
        {
            
        }

        public Universe Clone(Universe prototype)
        {
            SignatureIndexedFactSet edb = new SignatureIndexedFactSet(prototype.Edb.Count);
            edb.AddAll(prototype.Edb.All.Select(x => x.Clone()));

            Dictionary<string, HashSet<Rule>> idb = prototype.Idb.ToDictionary(x => x.Key, x => new HashSet<Rule>(x.Value.Select(y => y.Clone())));

            Universe clone = new Universe(engineInstance: this._engine, edb: edb, idb: idb);
            return clone;
        }
    }
}
