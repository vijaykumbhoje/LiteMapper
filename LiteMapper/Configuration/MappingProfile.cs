using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiteMapper.Configuration
{
    public abstract class MappingProfile
    {
        internal List<MappingDefinition> Definitions { get; } = new();

        protected MappingExpression<TSource, TDestination>
            CreateMap<TSource, TDestination>()
        {
            var definition = new MappingDefinition(
                typeof(TSource),
                typeof(TDestination));

            Definitions.Add(definition);

            return new MappingExpression<TSource, TDestination>(
                definition);
        }
    }
}
