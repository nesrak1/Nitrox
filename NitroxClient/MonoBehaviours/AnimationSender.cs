using UnityEngine;

namespace NitroxClient.MonoBehaviours
{
    public class AnimationSender : MonoBehaviour
    {
        //todo: better system for handling animation changes
        AnimChangeState lastUnderwaterState = AnimChangeState.UNSET;
        AnimChangeState lastStandingState = AnimChangeState.UNSET;
        AnimChangeState lastPdaState = AnimChangeState.UNSET;
        AnimChangeState lastUseToolState = AnimChangeState.UNSET;
        AnimChangeState lastUseToolAltState = AnimChangeState.UNSET;
        AnimChangeState lastPrecursorWaterState = AnimChangeState.UNSET;
        GUIHand guiHand;
        public void Start()
        {
            guiHand = Player.main.GetComponent<GUIHand>();
        }

        public void Update()
        {
            AnimChangeState underwaterState = (AnimChangeState)(Player.main.IsUnderwater() ? 1 : 0);
            AnimChangeState standingState = (AnimChangeState)(Player.main.playerController.activeController.grounded ? 1 : 0);
            AnimChangeState pdaState = (AnimChangeState)(Player.main.GetPDA().isInUse ? 1 : 0);
            AnimChangeState useToolState = (AnimChangeState)(guiHand.GetUsingTool() ? 1 : 0);
            AnimChangeState useToolAltState = (AnimChangeState)(guiHand.GetAltAttacking() ? 1 : 0);
            AnimChangeState precursorWaterState = (AnimChangeState)(Player.main.precursorOutOfWater ? 1 : 0);
            //todo: rework this into a method
            if (lastUnderwaterState != underwaterState)
            {
                Multiplayer.Logic.Player.AnimationChange(AnimChangeType.UNDERWATER, underwaterState);
                lastUnderwaterState = underwaterState;
            }
            if (lastStandingState != standingState)
            {
                Multiplayer.Logic.Player.AnimationChange(AnimChangeType.STANDING, standingState);
                lastStandingState = standingState;
            }
            if (lastPdaState != pdaState)
            {
                Multiplayer.Logic.Player.AnimationChange(AnimChangeType.PDAEQUIPPED, pdaState);
                lastPdaState = pdaState;
            }
            if (lastUseToolState != useToolState)
            {
                Multiplayer.Logic.Player.AnimationChange(AnimChangeType.USINGTOOL, useToolState);
                lastUseToolState = useToolState;
            }
            if (lastStandingState != useToolAltState)
            {
                Multiplayer.Logic.Player.AnimationChange(AnimChangeType.USINGTOOLALT, useToolAltState);
                lastStandingState = useToolAltState;
            }
            if (lastPrecursorWaterState != precursorWaterState)
            {
                Multiplayer.Logic.Player.AnimationChange(AnimChangeType.PRECURSORWATER, precursorWaterState);
                lastPrecursorWaterState = precursorWaterState;
            }
        }
    }

    public enum AnimChangeState
    {
        OFF,
        ON,
        UNSET
    }

    public enum AnimChangeType
    {
        UNDERWATER = 1, //underwater
        STANDING = 2, //on ground
        PDAEQUIPPED = 4, //pda out
        USINGTOOL = 8, //holding primary use button (ex attack with knife)
        USINGTOOLALT = 16, //holding alt use button (ex propulsion cannon drop) (not sure)
        PRECURSORWATER = 32, //in the "moonpool" under the qef
    }
}
