using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Simulation;
using MistakesLife.Core.Worlds;

namespace MistakesLife.Core.Life
{
    /// <summary>
    /// Fase de sistemas: lo que el tiempo hace con las relaciones, una vez al mes.
    /// <list type="bullet">
    /// <item>El rencor se olvida de a poco: <see cref="ResentmentFadePerMonth"/>
    /// punto al mes, el doble si la persona es amable (amabilidad desde
    /// <see cref="ForgivingAgreeableness"/>).</item>
    /// <item>Una amistad sin nada que la alimente se enfría: cada
    /// <see cref="FriendshipFadeMonths"/> meses pierde un punto de cariño, hasta
    /// <see cref="FriendshipFloor"/>. Los eventos (ayudar, compartir) la reavivan.</item>
    /// </list>
    /// No usa azar: lo que cambia viene de los eventos de los paquetes.
    /// </summary>
    public sealed class RelationshipSystem : IWeeklySystem
    {
        public const string SystemId = "sistema.relaciones";

        public const int ResentmentFadePerMonth = 1;

        public const int ForgivingAgreeableness = 60;

        public const int FriendshipFadeMonths = 4;

        public const int FriendshipFloor = 25;

        public string Id => SystemId;

        public WeekPhase Phase => WeekPhase.Systems;

        public void Tick(World world, WeekContext context)
        {
            if (!context.Date.IsFirstWeekOfMonth)
            {
                return;
            }

            bool friendshipsFade = context.Date.Month % FriendshipFadeMonths == 0;
            foreach (Character character in world.Characters.Where(c => c.IsAlive && c.Relationships.Count > 0))
            {
                int fade = ResentmentFadePerMonth * (character.Personality.Agreeableness >= ForgivingAgreeableness ? 2 : 1);
                foreach (Relationship relationship in character.Relationships)
                {
                    relationship.Resentment -= fade;
                    if (friendshipsFade && relationship.Bond == RelationshipBond.Friend && relationship.Affection > FriendshipFloor)
                    {
                        relationship.Affection -= 1;
                    }
                }
            }
        }
    }
}
