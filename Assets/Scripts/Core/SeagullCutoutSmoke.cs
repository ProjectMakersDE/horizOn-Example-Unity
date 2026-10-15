using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;

namespace SeagullStorm
{
    /// <summary>Runs only in the opt-in offline native-player smoke.</summary>
    public static class SeagullCutoutSmoke
    {
        public static bool Passed { get; private set; }

        public static IEnumerator Check()
        {
            Passed = false;
            var gm = GameManager.Instance;
            var player = PlayerController.Instance;
            var gull = player.GetComponent<SeagullCutoutRig>();
            if (!CheckRig(player.gameObject, gull, 7)) yield break;
            SpawnManager.Instance.enabled = false;
            EnemyPool.Instance.ReturnAll();
            var feather = player.GetComponent<FeatherThrow>();
            feather.enabled = false;
            yield return new WaitForSeconds(.7f);
            if (gull.CurrentMotion != SeagullCutoutRig.Motion.Idle) { Fail("Gull never returned to idle"); yield break; }
            var start = player.transform.position;
            var foot = Joint(player.gameObject, "leftFoot");
            var rotation = foot.localRotation;
            bool changed = false;
            for (int i = 0; i < 16; i++)
            {
                MovePlayerFixture(player);
                yield return new WaitForEndOfFrame();
                changed |= Quaternion.Angle(rotation, foot.localRotation) > 1f;
            }
            if (!changed || gull.CurrentMotion != SeagullCutoutRig.Motion.Walk) { Fail("Moving gull kept static joints"); yield break; }
            var source = player.GetComponent<SpriteRenderer>();
            source.flipX = true;
            yield return new WaitForEndOfFrame();
            if (player.transform.Find("Cutout rig/Facing and hop").localScale.x >= 0) { Fail("Gull facing did not mirror the rig"); yield break; }
            source.flipX = false;
            player.GetComponent<Rigidbody2D>().position = start;

            typeof(WeaponBase).GetField("_timer", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(feather, 0f);
            feather.enabled = true;
            yield return new WaitForSeconds(.2f);
            if (gull.CurrentMotion != SeagullCutoutRig.Motion.Attack || Quaternion.Angle(Quaternion.identity, Joint(player.gameObject, "frontWing").localRotation) < 5f)
            { Fail("Actual feather attack did not pose the gull wings"); yield break; }
            rotation = foot.localRotation;
            changed = false;
            for (int i = 0; i < 12; i++)
            {
                MovePlayerFixture(player);
                yield return new WaitForEndOfFrame();
                changed |= Quaternion.Angle(rotation, foot.localRotation) > 1f;
            }
            if (!changed || gull.CurrentMotion != SeagullCutoutRig.Motion.Attack)
            { Fail("Real auto-fire froze locomotion joints during movement"); yield break; }
            player.GetComponent<Rigidbody2D>().position = start;
            feather.enabled = false;
            ProjectilePool.Instance.ReturnAll();
            yield return new WaitForSeconds(.7f);

            var crab = EnemyPool.Instance.SpawnEnemy("crab", start + Vector3.right * 180, player.transform);
            yield return new WaitForSeconds(.15f);
            var crabRig = crab.GetComponent<SeagullCutoutRig>();
            if (!CheckRig(crab.gameObject, crabRig, 9)) yield break;
            var leg = Joint(crab.gameObject, "leftFront");
            rotation = leg.localRotation;
            yield return new WaitForSeconds(.2f);
            if (crabRig.CurrentMotion != SeagullCutoutRig.Motion.Walk || Quaternion.Angle(rotation, leg.localRotation) < 1f)
            { Fail("Pursuing crab kept static legs"); yield break; }
            if (crab.transform.Find("Cutout rig/Facing and hop").localScale.x <= 0) { Fail("Left-facing crab used the wrong art direction"); yield break; }

            crab.TakeHit(1);
            player.TakeDamage(1);
            yield return new WaitForEndOfFrame();
            if (crabRig.CurrentMotion != SeagullCutoutRig.Motion.Hurt || gull.CurrentMotion != SeagullCutoutRig.Motion.Hurt)
            { Fail("Actual damage did not enter hurt poses"); yield break; }
            if (Joint(crab.gameObject, "body").GetComponentInChildren<SpriteRenderer>().color != GameColors.Orange)
            { Fail("Damage tint did not reach cutout parts"); yield break; }
            yield return new WaitForSeconds(.5f);
            crab.transform.position = player.transform.position + Vector3.right * 5;
            yield return new WaitForSeconds(.15f);
            if (crabRig.CurrentMotion != SeagullCutoutRig.Motion.Attack || Quaternion.Angle(Quaternion.identity, Joint(crab.gameObject, "frontClaw").localRotation) < 1f)
            { Fail("Actual crab contact did not pose the claw"); yield break; }

            gm.ChangeState(GameState.Paused);
            var gullPaused = Joint(player.gameObject, "head").localRotation;
            var crabPaused = Joint(crab.gameObject, "frontClaw").localRotation;
            yield return new WaitForSecondsRealtime(.25f);
            if (Quaternion.Angle(gullPaused, Joint(player.gameObject, "head").localRotation) > .001f || Quaternion.Angle(crabPaused, Joint(crab.gameObject, "frontClaw").localRotation) > .001f)
            { Fail("Pause advanced cutout poses"); yield break; }
            gm.ChangeState(GameState.Run);
            EnemyPool.Instance.ReturnEnemy(crab);
            var reused = EnemyPool.Instance.SpawnEnemy("crab", start + Vector3.right * 180, player.transform);
            if (reused != crab || reused.GetComponent<SeagullCutoutRig>().CurrentMotion != SeagullCutoutRig.Motion.Idle || !CheckRig(reused.gameObject, crabRig, 9))
            { Fail("Crab pool reuse retained a hit/attack or duplicated parts"); yield break; }
            EnemyPool.Instance.ReturnEnemy(reused);
            gm.RunState.playerHP = gm.RunState.playerMaxHP;
            feather.enabled = true;
            SpawnManager.Instance.enabled = true;
            Passed = true;
            Debug.Log("[CUTOUT SMOKE] PASS: seven gull and nine crab parts, original renderer disabled, movement joints and movement during real auto-fire, correct facing, actual feather/contact attacks and damage, full-part tint, pause freeze and clean pool reuse.");
        }

        private static void MovePlayerFixture(PlayerController player)
        {
            // Drive both physics and rendered position, then observe after the rig's LateUpdate.
            // A physics-only teleport can remain invisible during several fast rendered frames.
            var body = player.GetComponent<Rigidbody2D>();
            Vector2 next = body.position + Vector2.right * 1.5f;
            body.position = next;
            player.transform.position = new Vector3(next.x, next.y, player.transform.position.z);
        }
        private static Transform Joint(GameObject actor, string name) => actor.transform.Find("Cutout rig/Facing and hop/Body lean/" + name);
        private static bool CheckRig(GameObject actor, SeagullCutoutRig rig, int count)
        {
            var visual = actor.transform.Find("Cutout rig");
            if (rig == null || !rig.Ready || visual == null || actor.GetComponent<SpriteRenderer>().enabled || visual.GetComponentsInChildren<SpriteRenderer>().Length != count + 1 || visual.GetComponent<SortingGroup>() == null)
            { Fail("Incomplete or duplicate cutout rig: " + actor.name); return false; }
            var animator = actor.GetComponent<Animator>();
            if (animator != null && animator.enabled) { Fail("Legacy animator still renders: " + actor.name); return false; }
            return true;
        }
        private static void Fail(string reason) { Debug.LogError("[CUTOUT SMOKE] FAIL: " + reason); Application.Quit(1); }
    }
}
