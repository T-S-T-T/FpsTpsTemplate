using UnityEngine;

namespace MovementTemplate
{
    /// <summary>
    /// Finds a ledge in front of the player and decides whether it is a vault (thin, low obstacle with
    /// ground behind it) or a mantle (climb up onto it).
    /// </summary>
    public static class LedgeProbe
    {
        public struct Info
        {
            public bool CanVault;
            public Vector3 Target;     // where a mantle ends (feet position on top of the ledge)
            public Vector3 Landing;    // where a vault ends (feet position past the obstacle)
            public float Height;       // ledge top above the player's feet
            public float TopY;
            public Vector3 WallNormal;
        }

        public static bool TryFind(PlayerMotor m, Vector3 dir, float maxHeight, float minHeight, out Info info)
        {
            info = default;
            MovementProfile p = m.profile;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) return false;
            dir.Normalize();

            Vector3 feet = m.transform.position;
            float r = m.Controller.radius;
            float reach = p.mantleReach + r;

            // 1. Find a near-vertical wall in front, sampling a few heights above the feet.
            RaycastHit wall = default;
            bool foundWall = false;
            for (int i = 0; i < 4; i++)
            {
                Vector3 origin = feet + Vector3.up * (0.1f + i * 0.4f);
                if (m.RaycastClosest(origin, dir, reach, out RaycastHit hit)
                    && hit.normal.y < 0.5f
                    && Vector3.Dot(dir, -hit.normal) > 0.5f)
                {
                    wall = hit;
                    foundWall = true;
                    break;
                }
            }
            if (!foundWall) return false;

            Vector3 normalFlat = MotorMath.Flat(wall.normal);
            if (normalFlat.sqrMagnitude < 0.0001f) return false;
            Vector3 inward = -normalFlat.normalized;

            // 2. Find the top surface just behind the wall face.
            float startY = feet.y + maxHeight + 0.1f;
            Vector3 topXZ = wall.point + inward * 0.08f;
            Vector3 rayOrigin = new Vector3(topXZ.x, startY, topXZ.z);
            float rayLength = (maxHeight + 0.1f) - minHeight + 0.02f;
            if (!m.RaycastClosest(rayOrigin, Vector3.down, rayLength, out RaycastHit top)) return false;
            if (top.normal.y < 0.7f) return false;

            float topY = top.point.y;
            float height = topY - feet.y;
            if (height < minHeight - 0.02f || height > maxHeight + 0.05f) return false;

            // 3. Find a spot on top where the player can stand, trying a few insets from the edge.
            Vector3 target = default;
            bool targetOk = false;
            for (int i = 0; i < 3 && !targetOk; i++)
            {
                float inset = i == 0 ? r + 0.1f : (i == 1 ? r * 0.5f : 0.1f);
                Vector3 xz = wall.point + inward * inset;
                Vector3 o = new Vector3(xz.x, topY + 0.3f, xz.z);
                if (!m.RaycastClosest(o, Vector3.down, 0.6f, out RaycastHit support)) continue;
                if (support.normal.y < 0.7f) continue;
                if (!m.StandingCapsuleFitsAt(support.point)) continue;
                target = support.point;
                targetOk = true;
            }
            if (!targetOk) return false;

            // 4. Vault check: is the obstacle thin enough, with safe ground behind it?
            bool canVault = false;
            Vector3 landing = target;
            if (height <= p.vaultMaxHeight)
            {
                float depth = 0.08f;
                bool ended = false;
                while (depth <= p.vaultMaxDepth)
                {
                    depth += 0.2f;
                    Vector3 xz = wall.point + inward * depth;
                    Vector3 o = new Vector3(xz.x, topY + 0.15f, xz.z);
                    bool onTop = m.RaycastClosest(o, Vector3.down, 0.35f, out RaycastHit h2) && h2.normal.y > 0.7f;
                    if (!onTop) { ended = true; break; }
                }

                if (ended)
                {
                    Vector3 xz = wall.point + inward * (depth + r + 0.15f);
                    Vector3 o = new Vector3(xz.x, topY + 0.2f, xz.z);
                    if (m.RaycastClosest(o, Vector3.down, 4f, out RaycastHit ground)
                        && ground.normal.y > 0.7f
                        && ground.point.y >= topY - 2.5f
                        && m.StandingCapsuleFitsAt(ground.point))
                    {
                        canVault = true;
                        landing = ground.point;
                    }
                }
            }

            info = new Info
            {
                CanVault = canVault,
                Target = target,
                Landing = landing,
                Height = height,
                TopY = topY,
                WallNormal = normalFlat.normalized
            };
            return true;
        }
    }
}
