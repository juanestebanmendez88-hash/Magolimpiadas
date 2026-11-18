using UnityEngine;

public class ControlBot : MageControl
{
    public Mage leader;
    public float distance = 1.6f;
    public float runDistance = 4f;
    public float maxDistance = 14f;
    public float stuckTime = 5f;

    public float jumpableHeight = 2.8f;
    public float arrowHeight = 2.1f;

    enum Step { Follow, Approach, Shoot, Raise }
    Step step;
    float stepStart;
    float wallX;
    int wallDirection;
    int attempts;
    bool shotFired, jumpDone;
    float floorBeforeRising;

    float lastAdvance, bestDistance = float.MaxValue;
    float farFrom = -1f;
    float fallingFrom = -1f;
    float jumpUntil;
    float forceX, forceUntil;
    int followSide = -1;
    readonly RaycastHit2D[] hits = new RaycastHit2D[8];

    Mage Leader()
    {
        if (leader != null) return leader;
        if (WorldSwitcher.Instance != null) return WorldSwitcher.Instance.player2D;
        return null;
    }

    void OnEnable() { Go(Step.Follow); lastAdvance = Time.time; bestDistance = float.MaxValue; farFrom = -1f; }

    void Go(Step p) { step = p; stepStart = Time.time; shotFired = jumpDone = false; }

    public override MageOrders ReadOrders(Mage self)
    {
        var o = new MageOrders();
        var l = Leader();
        o.aim = self.ShotPoint + (self.FacingRight ? Vector2.right : Vector2.left);
        if (l == null || l == self) return o;

        Vector2 pos = self.transform.position;
        Vector2 posL = l.transform.position;
        float t = Time.time - stepStart;

        if (CheckRespawn(self, l, pos, posL)) { Go(Step.Follow); forceUntil = 0f; return o; }

        switch (step)
        {
            case Step.Follow:
                if (Time.time < forceUntil && !(self.Grounded && Time.time > forceUntil - 0.6f))
                {
                    float fx = forceX - pos.x;
                    o.move = Mathf.Abs(fx) > 0.1f ? Mathf.Sign(fx) : 0f;
                }
                else if (Fight(self, posL, pos, ref o)) lastAdvance = Time.time;
                else if (GoToMyZone(self, posL, pos, ref o)) { }
                else if (GoToDoor(self, posL, pos, ref o)) lastAdvance = Time.time;
                else if (Climb(self, posL, pos, ref o)) { }
                else Follow(self, l, pos, posL, ref o);
                break;

            case Step.Approach:
            {
                float targetX = wallX - wallDirection * 1.05f;
                float dx = targetX - pos.x;
                o.move = Mathf.Abs(dx) > 0.12f ? Mathf.Clamp(dx * 2f, -1f, 1f) : 0f;
                if (self.Mode != MageMode.Bow && !self.Attacking) o.changeMode = true;
                if (Mathf.Abs(dx) <= 0.12f && Mathf.Abs(self.Speed.x) < 0.3f && self.Grounded && self.Mode == MageMode.Bow) Go(Step.Shoot);
                if (t > 3f) GiveUp();
                break;
            }

            case Step.Shoot:
                if (!shotFired && !self.Attacking)
                {
                    o.attackInput = true;
                    o.aim = new Vector2(wallX + wallDirection * 0.3f, pos.y + arrowHeight);
                    shotFired = true;
                }
                if (t > 0.55f && !self.Attacking) { floorBeforeRising = pos.y; Go(Step.Raise); }
                break;

            case Step.Raise:
            {
                if (!jumpDone) { o.jump = true; jumpDone = true; }
                o.holdJump = t < 0.5f;
                float targetX = wallX - wallDirection * 1.0f;
                if (t > 0.12f) o.move = Mathf.Clamp((targetX - pos.x) * 2f, -1f, 1f);
                if (t > 0.3f && self.Grounded)
                {
                    if (pos.y > floorBeforeRising + 1f) { attempts = 0; Go(Step.Follow); }
                    else if (++attempts >= 4) GiveUp();
                    else Go(Step.Approach);
                }
                if (t > 2.5f) Go(Step.Follow);
                break;
            }
        }
        if (Time.time < jumpUntil) o.holdJump = true;
        return o;
    }

    public float combatRadius = 9f;
    public float moveAwayMax = 12f;

    bool Fight(Mage self, Vector2 posL, Vector2 pos, ref MageOrders o)
    {
        if (FightBoss(self, posL, pos, ref o)) return true;
        Enemy bullseye = null, target = null;
        float bestBullseye = float.MaxValue, bestPoints = float.MaxValue;
        foreach (var e in Enemy.All)
        {
            if (e == null || !e.isActiveAndEnabled) continue;
            Vector2 c = e.Center;
            float dx = Mathf.Abs(c.x - pos.x), dy = Mathf.Abs(c.y - (pos.y + 1f));
            if (dx > combatRadius || dy > 2.5f || Vector2.Distance(c, posL) > moveAwayMax) continue;
            if (e.IsBullseye)
            {
                var j = Leader();
                bool playerCannot = j == null || !j.Alive || Vector2.Distance(j.transform.position, c) > 14f;
                if ((e.RemainingBullseye < 1.3f || playerCannot) && dx < bestBullseye) { bestBullseye = dx; bullseye = e; }
            }
            else if (e.CurrentState == Enemy.State.Active)
            {
                float mult = Weaknesses.Multiplier(self.data.power.element, e.family);
                if (mult <= 0f) continue;
                float points = dx + dy * 2f - (mult > 1f ? 6f : 0f);
                if (points < bestPoints) { bestPoints = points; target = e; }
            }
        }

        if (bullseye != null)
        {
            if (self.Mode != MageMode.Bow) { if (!self.Attacking) o.changeMode = true; return true; }
            if (!self.Attacking) { o.attackInput = true; o.aim = bullseye.Center; }
            return true;
        }
        if (target == null) return false;

        if (self.Mode != MageMode.Power) { if (!self.Attacking) o.changeMode = true; return true; }
        Vector2 ce = target.Center;
        float distX = ce.x - pos.x;
        if (Mathf.Abs(distX) > 5.5f) o.move = Mathf.Sign(distX);
        else if (Mathf.Abs(distX) < 2f && self.Grounded) o.move = -Mathf.Sign(distX) * 0.6f;
        if (o.move != 0f && self.Grounded && !Ground(self, pos + new Vector2(Mathf.Sign(o.move) * 0.9f, 0.3f), 2.5f)) o.move = 0f;
        float heightValue = ce.y - (pos.y + self.data.shotPoint.y);
        if (!self.Attacking && Mathf.Abs(heightValue) < 1.1f && Mathf.Abs(distX) < combatRadius) { o.attackInput = true; o.aim = ce; }
        else if (heightValue > 1.1f && heightValue < 3f && self.Grounded && Mathf.Abs(distX) < 6f) Skip(ref o, 0.2f);
        return true;
    }

    bool FightBoss(Mage self, Vector2 posL, Vector2 pos, ref MageOrders o)
    {
        var boss = DragonBoss.Current;
        if (boss == null || !boss.Active || Vector2.Distance(boss.Enemy.Center, posL) > 20f) return false;
        if (Weaknesses.Multiplier(self.data.power.element, boss.Enemy.family) <= 0f) return false;
        var pl = boss.platform;
        bool up = pos.x > pl.xMin - 0.35f && pos.x < pl.xMax + 0.35f && Mathf.Abs(pos.y - pl.yMax) < 0.35f && self.Grounded;
        if (!up)
        {
            if (!self.Grounded) { o.move = Mathf.Sign(pl.center.x - pos.x) * (Mathf.Abs(pl.center.x - pos.x) > 0.3f ? 1f : 0f); return true; }
            if (self.Mode != MageMode.Power && !self.Attacking) o.changeMode = true;
            float side = pos.x < pl.center.x ? pl.xMin - 0.8f : pl.xMax + 0.8f;
            float dx = side - pos.x;
            if (Mathf.Abs(dx) > 0.35f) o.move = Mathf.Sign(dx);
            else { Skip(ref o); forceX = pl.center.x; forceUntil = Time.time + 1.2f; }
            return true;
        }
        if (self.Mode != MageMode.Power) { if (!self.Attacking) o.changeMode = true; return true; }
        Vector2 c = boss.Enemy.Center;
        float heightValue = c.y - (pos.y + self.data.shotPoint.y);
        if (!self.Attacking && Mathf.Abs(heightValue) < 1.2f) { o.attackInput = true; o.aim = c; }
        return true;
    }

    bool GoToMyZone(Mage self, Vector2 posL, Vector2 pos, ref MageOrders o)
    {
        foreach (var z in PowerZone.All)
        {
            if (!z.Ready || !z.BelongsTo(self) || Vector2.Distance(z.Area.center, posL) > 14f) continue;
            if (z.Contains(self)) { if (self.Grounded && !self.Attacking) o.useZonePower = true; return true; }
            float dx = z.Area.center.x - pos.x;
            o.move = Mathf.Sign(dx);
            o.run = Mathf.Abs(dx) > runDistance;
            int dir = (int)Mathf.Sign(dx);
            if (self.Grounded && Wall(self, pos, dir, 0.3f, 1.2f, out _) && WallHeight(self, pos, dir) <= jumpableHeight) Skip(ref o);
            else if (self.Grounded && z.Area.min.y > pos.y + 1f && Mathf.Abs(dx) < 3f) Skip(ref o);
            return true;
        }
        return false;
    }

    float nextDoorShot;
    bool GoToDoor(Mage self, Vector2 posL, Vector2 pos, ref MageOrders o)
    {
        var p = ElementalDoor.Current;
        if (p == null || Vector2.Distance(p.transform.position, posL) > 16f) return false;
        var leader = Leader();
        if (!p.IsOpen)
        {
            var leaderCircle = leader != null ? p.CircleOf(leader.data.element) : null;
            if (leaderCircle != null && !leaderCircle.Lit) return false;
            var c = p.CircleOf(self.data.element);
            if (c == null || c.Lit) return false;
            float x = c.transform.position.x - 4.5f;
            float dx = x - pos.x;
            if (Mathf.Abs(dx) > 0.4f)
            {
                o.move = Mathf.Sign(dx);
                int dir = (int)Mathf.Sign(dx);
                if (self.Grounded && Wall(self, pos, dir, 0.3f, 1.2f, out _) && WallHeight(self, pos, dir) <= jumpableHeight) Skip(ref o);
                return true;
            }
            if (self.Mode != MageMode.Bow) { if (!self.Attacking) o.changeMode = true; return true; }
            if (!self.Attacking && self.Grounded && Time.time > nextDoorShot)
            {
                o.attackInput = true;
                o.aim = c.transform.position;
                nextDoorShot = Time.time + 1f;
            }
            return true;
        }
        var leaderRune = leader != null ? p.RuneOf(leader.data.element) : null;
        if (leaderRune != null && !leaderRune.litUp) return false;
        var r = p.RuneOf(self.data.element);
        if (r == null) return false;
        float rx = r.point.x - pos.x;
        o.move = Mathf.Abs(rx) > 0.25f ? Mathf.Sign(rx) : 0f;
        if (o.move != 0f && self.Grounded && r.point.y > pos.y + 0.5f)
        {
            int dir = (int)Mathf.Sign(rx);
            if (Wall(self, pos, dir, 0.3f, 1.2f, out _) || Mathf.Abs(rx) < 2.5f) { Skip(ref o, 0.25f); forceX = r.point.x; forceUntil = Time.time + 0.9f; }
        }
        if (self.Mode != MageMode.Power && !self.Attacking && Mathf.Abs(rx) <= 0.25f) o.changeMode = true;
        return true;
    }

    bool Climb(Mage self, Vector2 posL, Vector2 pos, ref MageOrders o)
    {
        if (posL.y < pos.y + 1.5f && !self.Climbing) return false;
        var e = Vine.Nearest(pos, 6f);
        if (e == null) return false;
        float dx = e.Area.center.x - pos.x;
        if (!e.Touches(self)) { o.move = Mathf.Abs(dx) > 0.15f ? Mathf.Sign(dx) : 0f; return true; }
        o.holdJump = true;
        jumpUntil = Time.time + 0.1f;
        if (pos.y > e.Cap - 0.6f) o.move = Mathf.Sign(posL.x - pos.x);
        return true;
    }

    bool PortalTowardLeader(Vector2 pos, Vector2 posL, out float portalX)
    {
        portalX = 0f;
        float direct = Vector2.Distance(pos, posL);
        foreach (var p in Portal.All)
        {
            if (!p.lit || p.destination == null || !p.destination.lit) continue;
            float byPortal = Vector2.Distance(pos, p.transform.position) + Vector2.Distance(p.destination.Exit, posL);
            if (byPortal + 3f < direct && Mathf.Abs(p.transform.position.y - pos.y) < 3f) { portalX = p.transform.position.x; return true; }
        }
        return false;
    }

    void Skip(ref MageOrders o, float duration = 0.45f)
    {
        o.jump = true;
        o.holdJump = true;
        jumpUntil = Time.time + duration;
    }

    void Follow(Mage self, Mage l, Vector2 pos, Vector2 posL, ref MageOrders o)
    {
        if (Mathf.Abs(l.Speed.x) > 1f) followSide = l.Speed.x > 0f ? -1 : 1;
        bool leaderAbove = posL.y > pos.y + 1.5f;
        float targetX = leaderAbove && Mathf.Abs(posL.x - pos.x) < 7f ? posL.x : posL.x + followSide * distance;
        if (PortalTowardLeader(pos, posL, out float portalX)) { targetX = portalX; leaderAbove = false; }
        float dx = targetX - pos.x;
        float near = 0.35f;
        bool farHorizontal = Mathf.Abs(dx) > near;

        if (self.Mode == MageMode.Bow && !self.Attacking && !leaderAbove) o.changeMode = true;

        if (farHorizontal)
        {
            o.move = Mathf.Sign(dx);
            o.run = Mathf.Abs(dx) > runDistance;
        }
        int dir = o.move > 0.01f ? 1 : o.move < -0.01f ? -1 : 0;
        if (dir == 0 && leaderAbove) dir = posL.x >= pos.x ? 1 : -1;
        if (dir == 0 || !self.Grounded) return;

        if (Wall(self, pos, dir, 0.3f, 1.2f, out float faceX))
        {
            float heightValue = WallHeight(self, pos, dir);
            if (heightValue <= jumpableHeight)
            {
                Skip(ref o);
                forceX = faceX + dir * 0.9f;
                forceUntil = Time.time + 1.1f;
            }
            else if (IsClimbable(self, pos, dir, faceX) && (leaderAbove || farHorizontal))
            {
                wallX = faceX; wallDirection = dir; attempts = 0;
                Go(Step.Approach);
            }
            return;
        }

        if (!Ground(self, pos + new Vector2(dir * 0.9f, 0.3f), 2.5f))
        {
            bool leaderAhead = Mathf.Sign(posL.x - pos.x) == dir && Mathf.Abs(posL.x - pos.x) > 1.5f;
            bool leaderBelow = posL.y < pos.y - 1f;
            if (!leaderAhead && !leaderBelow) { o.move = 0f; o.run = false; return; }
            if (leaderAhead && !leaderBelow) Skip(ref o);
            return;
        }

        if (leaderAbove && posL.y - pos.y <= jumpableHeight + 0.4f && Mathf.Abs(posL.x - pos.x) < 3.5f)
        {
            Skip(ref o);
            forceX = posL.x;
            forceUntil = Time.time + 1.1f;
        }
    }

    void GiveUp()
    {
        attempts = 0;
        Go(Step.Follow);
        bestDistance = float.MaxValue;
        lastAdvance = Time.time - stuckTime;
    }

    bool CheckRespawn(Mage self, Mage l, Vector2 pos, Vector2 posL)
    {
        float d = Vector2.Distance(pos, posL);
        bool near = Mathf.Abs(pos.x - posL.x) <= distance + 1f && Mathf.Abs(pos.y - posL.y) < 1.5f;
        if (near || d < bestDistance - 0.5f) { bestDistance = d; lastAdvance = Time.time; }
        if (near) bestDistance = float.MaxValue;

        if (d > maxDistance) { if (farFrom < 0f) farFrom = Time.time; } else farFrom = -1f;
        bool veryFar = farFrom >= 0f && Time.time - farFrom > 1.5f;
        if (!self.Grounded && self.Speed.y < -1f) { if (fallingFrom < 0f) fallingFrom = Time.time; } else fallingFrom = -1f;
        bool fell = fallingFrom >= 0f && Time.time - fallingFrom > 1.5f && pos.y < posL.y - 4f;
        bool stuck = !near && Time.time - lastAdvance > stuckTime;

        if (!(veryFar || fell || stuck) || !l.Grounded) return false;
        Debug.Log(self.data.displayName + " aparece al lado del líder (" + (fell ? "se cayó" : veryFar ? "quedó muy lejos" : "estaba atascado") + ")");
        Vector2 place = posL + new Vector2(followSide * distance, 0.05f);
        if (!Free(self, place)) place = posL + new Vector2(followSide * 0.8f, 0.05f);
        if (!Free(self, place)) place = posL + new Vector2(0f, 0.05f);
        self.Place(place);
        lastAdvance = Time.time; bestDistance = float.MaxValue; farFrom = -1f; fallingFrom = -1f;
        return true;
    }

    bool Lightning(Mage self, Vector2 fromPoint, Vector2 dir, float length, out RaycastHit2D hit)
    {
        var filter = new ContactFilter2D { useTriggers = false };
        int n = Physics2D.Raycast(fromPoint, dir, filter, hits, length);
        hit = default;
        float best = float.MaxValue;
        for (int i = 0; i < n; i++)
        {
            var c = hits[i].collider;
            if (c.GetComponentInParent<Mage>() != null || c.GetComponentInParent<Arrow>() != null) continue;
            if (hits[i].distance < best) { best = hits[i].distance; hit = hits[i]; }
        }
        return best < float.MaxValue;
    }

    bool Wall(Mage self, Vector2 feet, int dir, float fromHeight, float length, out float faceX)
    {
        faceX = 0f;
        foreach (float h in new[] { fromHeight, 1.0f, 1.8f })
            if (Lightning(self, feet + Vector2.up * h, Vector2.right * dir, length, out var g) && Mathf.Abs(g.normal.x) > 0.7f)
            {
                faceX = g.point.x;
                return true;
            }
        return false;
    }

    float WallHeight(Mage self, Vector2 feet, int dir)
    {
        for (float h = 0.3f; h <= 12f; h += 0.25f)
            if (!Lightning(self, feet + Vector2.up * h, Vector2.right * dir, 1.8f, out _)) return h;
        return 99f;
    }

    bool IsClimbable(Mage self, Vector2 feet, int dir, float faceX)
    {
        if (!Lightning(self, feet + Vector2.up * arrowHeight, Vector2.right * dir, Mathf.Abs(faceX - feet.x) + 1.5f, out var g)) return false;
        var mark = g.collider.GetComponentInParent<ClimbableSurface>();
        if (mark != null) return mark.climbable;
        var walls = g.collider.GetComponent<ClimbableWalls>();
        return walls != null && walls.IsClimbable(g.point, g.normal);
    }

    bool Ground(Mage self, Vector2 fromPoint, float length)
    {
        return Lightning(self, fromPoint, Vector2.down, length, out var g) && g.normal.y > 0.5f;
    }

    bool Free(Mage self, Vector2 feet)
    {
        var filter = new ContactFilter2D { useTriggers = false };
        var res = new Collider2D[4];
        int n = Physics2D.OverlapCapsule(feet + Vector2.up * 1.1f, new Vector2(0.7f, 1.9f), CapsuleDirection2D.Vertical, 0f, filter, res);
        for (int i = 0; i < n; i++)
            if (res[i].GetComponentInParent<Mage>() == null && res[i].GetComponentInParent<Arrow>() == null) return false;
        return Ground(self, feet + Vector2.up * 0.3f, 1.5f);
    }
}
