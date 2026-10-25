using System;
using System.Collections;
using BoxPushing.Core;
using UnityEngine;

namespace BoxPushing.Gameplay
{
    /// <summary>
    /// Plays a <see cref="MoveDelta"/> as a short transform animation. During a push the
    /// player and box stay locked in step and share the push curve; after arriving the
    /// player's face turns with the turn curve. Input is locked while
    /// <see cref="IsAnimating"/> is true.
    /// </summary>
    public class MoveAnimator : MonoBehaviour
    {
        private Coroutine current;

        public bool IsAnimating => current != null;

        public void Play(MoveDelta delta, PlayerView player, BoardView board, MoveAnimationConfig config, Action onComplete)
        {
            if (current != null || player == null || board == null || config == null)
                return;

            current = StartCoroutine(Animate(delta, player, board, config, onComplete));
        }

        private IEnumerator Animate(MoveDelta delta, PlayerView player, BoardView board, MoveAnimationConfig config, Action onComplete)
        {
            float duration = delta.PushedBox ? config.pushDuration : config.walkDuration;
            AnimationCurve moveCurve = delta.PushedBox ? config.pushCurve : config.walkCurve;

            Vector3 playerFrom = BoardGeometry.CellToWorld(delta.PlayerFrom, board.Width, board.Height);
            Vector3 playerTo = BoardGeometry.CellToWorld(delta.PlayerTo, board.Width, board.Height);

            GameObject box = delta.PushedBox ? board.GetBoxAt(delta.BoxFrom.Value) : null;
            Vector3 boxFrom = default;
            Vector3 boxTo = default;
            if (box != null)
            {
                float boxY = box.transform.position.y;
                boxFrom = BoardGeometry.CellToWorld(delta.BoxFrom.Value, board.Width, board.Height) + new Vector3(0f, boxY, 0f);
                boxTo = BoardGeometry.CellToWorld(delta.BoxTo.Value, board.Width, board.Height) + new Vector3(0f, boxY, 0f);
            }

            yield return AnimateMove(player, box, playerFrom, playerTo, boxFrom, boxTo, duration, moveCurve);
            yield return AnimateTurn(player, delta.FacingFrom, delta.FacingTo, config.turnDuration, config.turnCurve);

            player.SetFacing(delta.FacingTo);
            current = null;
            onComplete?.Invoke();
        }

        private static IEnumerator AnimateMove(
            PlayerView player,
            GameObject box,
            Vector3 playerFrom,
            Vector3 playerTo,
            Vector3 boxFrom,
            Vector3 boxTo,
            float duration,
            AnimationCurve curve)
        {
            if (player != null)
                player.transform.position = playerFrom;
            if (box != null)
                box.transform.position = boxFrom;

            if (duration <= 0f)
            {
                if (player != null)
                    player.transform.position = playerTo;
                if (box != null)
                    box.transform.position = boxTo;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float k = Mathf.Clamp01(elapsed / duration);
                float s = curve != null ? curve.Evaluate(k) : k;
                if (player != null)
                    player.transform.position = Vector3.LerpUnclamped(playerFrom, playerTo, s);
                if (box != null)
                    box.transform.position = Vector3.LerpUnclamped(boxFrom, boxTo, s);
                yield return null;
            }

            if (player != null)
                player.transform.position = playerTo;
            if (box != null)
                box.transform.position = boxTo;
        }

        private static IEnumerator AnimateTurn(PlayerView player, Direction facingFrom, Direction facingTo, float duration, AnimationCurve curve)
        {
            if (player == null)
                yield break;

            float fromAngle = BoardGeometry.ToYRotation(facingFrom);
            float toAngle = BoardGeometry.ToYRotation(facingTo);

            if (duration <= 0f)
            {
                player.SetFacingAngle(toAngle);
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float k = Mathf.Clamp01(elapsed / duration);
                float s = curve != null ? curve.Evaluate(k) : k;
                player.SetFacingAngle(Mathf.LerpAngle(fromAngle, toAngle, s));
                yield return null;
            }

            player.SetFacingAngle(toAngle);
        }
    }
}
