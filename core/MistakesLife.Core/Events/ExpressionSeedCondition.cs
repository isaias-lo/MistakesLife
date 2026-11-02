using MistakesLife.Core.Content;
using MistakesLife.Core.Scripting;
using MistakesLife.Core.Seeds;
using MistakesLife.Core.Worlds;

namespace MistakesLife.Core.Events
{
    /// <summary>Condición de una semilla escrita en el lenguaje de los paquetes.</summary>
    public sealed class ExpressionSeedCondition : ISeedCondition
    {
        private readonly Expression _expression;
        private readonly ContentDatabase _content;

        public ExpressionSeedCondition(Expression expression, ContentDatabase content)
        {
            _expression = expression;
            _content = content;
        }

        public bool IsMet(World world, Seed seed)
        {
            if (!world.TryGetCharacter(seed.OwnerId, out var owner))
            {
                return false;
            }

            return _expression.IsTrue(new EventScope(world, _content, owner, seed, readOnly: true));
        }
    }
}
