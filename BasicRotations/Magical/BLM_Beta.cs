﻿namespace RebornRotations.Magical;

[Rotation("Beta", CombatType.PvE, GameVersion = "7.15")]
[SourceCode(Path = "main/BasicRotations/Magical/BLM_Beta.cs")]
[Api(4)]

public sealed class BLM_Beta : BlackMageRotation
{
    #region Config Options
    [RotationConfig(CombatType.PvE, Name = "Use Infinite Paradox Rotation (Requires Level 90)")]
    public bool Infinity { get; set; } = false;

    [RotationConfig(CombatType.PvE, Name = "(Standard Rotation) Use Leylines in combat when standing still")]
    public bool LeylineMadness { get; set; } = false;

    [RotationConfig(CombatType.PvE, Name = "(Standard Rotation) Use both stacks of Leylines automatically")]
    public bool Leyline2Madness { get; set; } = false;

    [RotationConfig(CombatType.PvE, Name = "(Standard Rotation) Use Retrace when out of Leylines in combat and standing still")]
    public bool UseRetrace { get; set; } = false;

    [RotationConfig(CombatType.PvE, Name = "(Infinite Paradox Rotation) Use Gemdraught/Tincture/pot")]
    public bool UseMedicine { get; set; } = false;
    #endregion

    public bool NextGCDisInstant => Player.HasStatus(true, StatusID.Triplecast, StatusID.Swiftcast);

    public bool CanMakeInstant => TriplecastPvE.Cooldown.CurrentCharges > 0 || !SwiftcastPvE.Cooldown.IsCoolingDown;

    public int ThisManyInstantCasts => (TriplecastPvE.Cooldown.CurrentCharges * 3) + Player.StatusStack(true, StatusID.Triplecast) + SwiftcastPvE.Cooldown.CurrentCharges;

    public int AstralDefecit => ThisManyInstantCasts - AstralSoulStacks;

    protected override IAction? CountDownAction(float remainTime)
    {
        IAction act;
        if (remainTime < FireIiiPvE.Info.CastTime + CountDownAhead)
        {
            if (FireIiiPvE.CanUse(out act)) return act;
        }
        return base.CountDownAction(remainTime);
    }

    #region Additional oGCD Logic

    [RotationDesc]
    protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
    {
        if (!Infinity)
        {
            //To Fire
            if (CurrentMp >= 7200 && UmbralIceStacks == 2 && ParadoxPvE.EnoughLevel)
            {
                if ((HasFire || HasSwift) && TransposePvE.CanUse(out act)) return true;
            }
            if (nextGCD.IsTheSameTo(false, FireIiiPvE) && HasFire)
            {
                if (TransposePvE.CanUse(out act)) return true;
            }

            //Using Manafont
            if (InAstralFire)
            {
                if ((CurrentMp == 0 || (!FireIiiPvE.EnoughLevel && CurrentMp <= 1600)) && ManafontPvE.CanUse(out act)) return true;
                //To Ice
                if (NeedToTransposeGoIce(true) && TransposePvE.CanUse(out act)) return true;
            }
        }

        return base.EmergencyAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.AetherialManipulationPvE)]
    protected override bool MoveForwardAbility(IAction nextGCD, out IAction? act)
    {
        if (AetherialManipulationPvE.CanUse(out act)) return true;
        return base.MoveForwardAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.BetweenTheLinesPvE)]
    protected override bool MoveBackAbility(IAction nextGCD, out IAction? act)
    {
        if (BetweenTheLinesPvE.CanUse(out act)) return true;
        return base.MoveBackAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.ManawardPvE)]
    protected sealed override bool DefenseAreaAbility(IAction nextGCD, out IAction? act)
    {
        if (ManawardPvE.CanUse(out act)) return true;
        return base.DefenseAreaAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.ManawardPvE, ActionID.AddlePvE)]
    protected override bool DefenseSingleAbility(IAction nextGCD, out IAction? act)
    {
        if (ManawardPvE.CanUse(out act)) return true;
        if (AddlePvE.CanUse(out act)) return true;
        return base.DefenseSingleAbility(nextGCD, out act);
    }
    #endregion

    #region oGCD Logic

    [RotationDesc(ActionID.TransposePvE, ActionID.LeyLinesPvE, ActionID.RetracePvE)]
    protected override bool GeneralAbility(IAction nextGCD, out IAction? act)
    {
        if (!Infinity)
        {
            if (InCombat && IsMoving && HasHostilesInRange && TriplecastPvE.CanUse(out act, usedUp: true)) return true;
            if (LeylineMadness && InCombat && HasHostilesInRange && LeyLinesPvE.CanUse(out act, usedUp: Leyline2Madness)) return true;
            if (!IsLastAbility(ActionID.LeyLinesPvE) && UseRetrace && InCombat && HasHostilesInRange && RetracePvE.CanUse(out act)) return true;
        }

        return base.GeneralAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.RetracePvE, ActionID.SwiftcastPvE, ActionID.TriplecastPvE, ActionID.AmplifierPvE)]
    protected override bool AttackAbility(IAction nextGCD, out IAction? act)
    {
        if (!Infinity)
        {
            if (InUmbralIce)
            {
                if (UmbralIceStacks == 2 && !HasFire
                    && !IsLastGCD(ActionID.ParadoxPvE))
                {
                    if (SwiftcastPvE.CanUse(out act)) return true;
                    if (InCombat && TriplecastPvE.CanUse(out act, usedUp: true)) return true;
                }

                if (UmbralIceStacks < 3 && LucidDreamingPvE.CanUse(out act)) return true;
            }

            if (InAstralFire)
            {
                if (InCombat && TriplecastPvE.CanUse(out act, gcdCountForAbility: 5)) return true;
            }

            if (AmplifierPvE.CanUse(out act)) return true;
        }

        if (ParadoxPvE.EnoughLevel && Infinity)
        {
            if (UseMedicine && UseBurstMedicine(out act)) return true;

            if (ElementTime > 0 && ElementTimeEndAfter(1))
            {
                if (TransposePvE.CanUse(out act)) return true;
            }

            if (InCombat)
            {
                if (ThisManyInstantCasts > 6 && AstralSoulStacks < 6 && InAstralFire && ElementTime > 6 && Player.HasStatus(true, StatusID.Firestarter))
                {
                    if (ManafontPvE.CanUse(out act)) return true;
                }
                if (ThisManyInstantCasts > 6 && AstralSoulStacks < 6 && InAstralFire && ElementTime > 6 && Player.HasStatus(true, StatusID.Firestarter) && Player.CurrentMp > 9000)
                {
                    if (InCombat && TriplecastPvE.CanUse(out act)) return true;
                }

                if (ThisManyInstantCasts > 3 && AstralSoulStacks == 3 && InAstralFire && ElementTime > 6)
                {
                    if (InCombat && TriplecastPvE.CanUse(out act, usedUp: true)) return true;
                }

                if (ThisManyInstantCasts == 1 && AstralSoulStacks == 6 && InAstralFire && ElementTime > 3)
                {
                    if (SwiftcastPvE.CanUse(out act)) return true;
                }

                if (LeyLinesPvE.CanUse(out act)) return true;

                if (!IsPolyglotStacksMaxed)
                {
                    if (AmplifierPvE.CanUse(out act)) return true;
                }
            }
        }

        return base.AttackAbility(nextGCD, out act);
    }
    #endregion

    #region GCD Logic

    protected override bool GeneralGCD(out IAction? act)
    {
        if (!Infinity)
        {
            if (FlareStarPvE.CanUse(out act)) return true;

            if (InFireOrIce(out act, out var mustGo)) return true;
            if (mustGo) return false;

            if (AddElementBase(out act)) return true;
            if (ScathePvE.CanUse(out act)) return true;
            if (MaintainStatus(out act)) return true;
        }

        if (ParadoxPvE.EnoughLevel && Infinity)
        {

            if (InAstralFire && ElementTime < 4 && Player.HasStatus(true, StatusID.Firestarter))
            {
                if (FireIiiPvE.CanUse(out act)) return true;
            }

            if (NextGCDisInstant)
            {
                if (AstralSoulStacks == 6)
                {
                    if (FlareStarPvE.CanUse(out act)) return true;
                }
                if (FireIvPvE.CanUse(out act)) return true;
            }

            if (HostileTarget != null && (!HostileTarget.HasStatus(true, StatusID.Thunder, StatusID.ThunderIi, StatusID.ThunderIii, StatusID.ThunderIv, StatusID.HighThunder_3872, StatusID.HighThunder) || HostileTarget.StatusTime(true, StatusID.Thunder, StatusID.ThunderIi, StatusID.ThunderIii, StatusID.ThunderIv, StatusID.HighThunder_3872, StatusID.HighThunder) < 3))
            {
                if (ThunderPvE.CanUse(out act)) return true;
            }

            if (!NextGCDisInstant)
            {
                if (FoulPvE.CanUse(out act)) return true;
                if (XenoglossyPvE.CanUse(out act)) return true;
            }

            if (IsParadoxActive)
            {
                if (ParadoxPvE.CanUse(out act)) return true;
            }

            if (InCombat && UmbralIceStacks == 3 && UmbralHearts == 3 && InUmbralIce)
            {
                if (TransposePvE.CanUse(out act)) return true;
            }

            if (InAstralFire && AstralFireStacks == 3 && !Player.HasStatus(true, StatusID.Firestarter) && CurrentMp < 800)
            {
                if (TransposePvE.CanUse(out act)) return true;
            }

            if (InAstralFire)
            {
                if (Player.HasStatus(true, StatusID.Firestarter))
                {
                    if (FireIiiPvE.CanUse(out act)) return true;
                }
                if (CurrentMp >= 800)
                {
                    if (DespairPvE.CanUse(out act)) return true;
                }
            }

            if (InUmbralIce)
            {
                if (UmbralSoulPvE.CanUse(out act, skipCastingCheck: true)) return true;
            }

            if (!InUmbralIce && !InAstralFire)
            {
                if (NextGCDisInstant)
                {
                    if (FireIiiPvE.CanUse(out act)) return true;
                }
            }

            if (BlizzardIiiPvE.CanUse(out act)) return true;
        }

        return base.GeneralGCD(out act);
    }

    #endregion

    #region Black Magic

    private bool InFireOrIce(out IAction? act, out bool mustGo)
    {
        act = null;
        mustGo = false;
        if (InUmbralIce)
        {
            if (GoFire(out act)) return true;
            if (MaintainIce(out act)) return true;
            if (DoIce(out act)) return true;
        }
        if (InAstralFire)
        {
            if (GoIce(out act)) return true;
            if (MaintainFire(out act)) return true;
            if (DoFire(out act)) return true;
        }
        return false;
    }

    private bool GoIce(out IAction? act)
    {
        act = null;

        if (!NeedToGoIce) return false;

        //Use Manafont or transpose.
        if ((!ManafontPvE.Cooldown.IsCoolingDown || NeedToTransposeGoIce(false))
            && UseInstanceSpell(out act)) return true;

        //Go to Ice.
        if (BlizzardIiPvE.CanUse(out act)) return true;
        if (BlizzardIiiPvE.CanUse(out act)) return true;
        if (TransposePvE.CanUse(out act)) return true;
        if (BlizzardPvE.CanUse(out act)) return true;
        return false;
    }

    private bool MaintainIce(out IAction? act)
    {
        act = null;
        if (UmbralIceStacks == 1)
        {
            if (BlizzardIiPvE.CanUse(out act)) return true;

            if (Player.Level == 90 && BlizzardPvE.CanUse(out act)) return true;
            if (BlizzardIiiPvE.CanUse(out act)) return true;
        }
        if (UmbralIceStacks == 2 && Player.Level < 90)
        {
            if (BlizzardIiPvE.CanUse(out act)) return true;
            if (BlizzardPvE.CanUse(out act)) return true;
        }
        return false;
    }

    private bool DoIce(out IAction? act)
    {
        act = null;

        if (IsLastAction(ActionID.UmbralSoulPvE, ActionID.TransposePvE)
            && IsParadoxActive && BlizzardPvE.CanUse(out act)) return true;

        if (UmbralIceStacks == 3 && UsePolyglot(out act)) return true;

        //Add Hearts
        if (UmbralIceStacks == 3 &&
            BlizzardIvPvE.EnoughLevel && UmbralHearts < 3 && !IsLastGCD
            (ActionID.BlizzardIvPvE, ActionID.FreezePvE))
        {
            if (FreezePvE.CanUse(out act)) return true;
            if (BlizzardIvPvE.CanUse(out act)) return true;
        }

        if (AddThunder(out act, 5)) return true;
        if (UmbralIceStacks == 2 && UsePolyglot(out act, 0)) return true;

        if (IsParadoxActive)
        {
            if (BlizzardPvE.CanUse(out act)) return true;
        }

        if (BlizzardIiPvE.CanUse(out act)) return true;
        if (BlizzardIvPvE.CanUse(out act)) return true;
        if (BlizzardPvE.CanUse(out act)) return true;
        return false;
    }

    private bool GoFire(out IAction? act)
    {
        act = null;

        //Transpose line
        if (UmbralIceStacks < 3) return false;

        //Need more MP
        if (CurrentMp < 9600) return false;

        if (IsParadoxActive)
        {
            if (BlizzardPvE.CanUse(out act)) return true;
        }

        //Go to Fire.
        if (FireIiPvE.CanUse(out act)) return true;
        if (FireIiiPvE.CanUse(out act)) return true;
        if (TransposePvE.CanUse(out act)) return true;
        if (FirePvE.CanUse(out act)) return true;

        return false;
    }

    private bool MaintainFire(out IAction? act)
    {
        act = null;
        switch (AstralFireStacks)
        {
            case 1:
                if (FireIiPvE.CanUse(out act)) return true;
                if (FireIiiPvE.CanUse(out act)) return true;
                break;
            case 2:
                if (FireIiPvE.CanUse(out act)) return true;
                if (FirePvE.CanUse(out act)) return true;
                break;
        }

        if (ElementTimeEndAfterGCD(false ? 3u : 2u))
        {
            if (CurrentMp >= FirePvE.Info.MPNeed * 2 + 800 && FirePvE.CanUse(out act)) return true;
            if (FlarePvE.CanUse(out act)) return true;
            if (DespairPvE.CanUse(out act)) return true;
        }

        return false;
    }

    private bool DoFire(out IAction? act)
    {
        act = null;
        if (UsePolyglot(out act)) return true;

        // Add thunder only at combat start.
        if (CombatElapsedLess(5))
        {
            if (AddThunder(out act, 0)) return true;
        }

        if (InCombat && TriplecastPvE.CanUse(out act)) return true;

        if (AddThunder(out act, 0) && Player.WillStatusEndGCD(1, 0, true,
            StatusID.Thundercloud)) return true;

        if (UmbralHearts < 2 && FlarePvE.CanUse(out act)) return true;
        if (FireIiPvE.CanUse(out act)) return true;

        if (CurrentMp >= FirePvE.Info.MPNeed + 800)
        {
            if (FireIvPvE.EnoughLevel)
            {
                if (FireIvPvE.CanUse(out act)) return true;
            }
            else if (HasFire)
            {
                if (FireIiiPvE.CanUse(out act)) return true;
            }
            if (FirePvE.CanUse(out act)) return true;
        }

        if (DespairPvE.CanUse(out act)) return true;

        return false;
    }

    private bool UseInstanceSpell(out IAction? act)
    {
        act = null;
        if (UsePolyglot(out act)) return true;
        if (HasThunder && AddThunder(out act, 1)) return true;
        if (UsePolyglot(out act, 0)) return true;
        return false;
    }

    private bool AddThunder(out IAction? act, uint gcdCount = 3)
    {
        act = null;
        //Return if just used.
        if (IsLastGCD(ActionID.ThunderPvE, ActionID.ThunderIiPvE, ActionID.ThunderIiiPvE, ActionID.ThunderIvPvE)) return false;

        //So long for thunder.
        if (ThunderPvE.CanUse(out _) && (!ThunderPvE.Target.Target?.WillStatusEndGCD(gcdCount, 0, true,
            StatusID.Thunder, StatusID.ThunderIi, StatusID.ThunderIii, StatusID.ThunderIv, StatusID.HighThunder_3872) ?? false))
            return false;

        if (ThunderIiPvE.CanUse(out act)) return true;
        if (ThunderPvE.CanUse(out act)) return true;

        return false;
    }

    private bool AddElementBase(out IAction? act)
    {
        act = null;
        if (CurrentMp >= 7200)
        {
            if (FireIiPvE.CanUse(out act)) return true;
            if (FireIiiPvE.CanUse(out act)) return true;
            if (FirePvE.CanUse(out act)) return true;
        }
        else
        {
            if (BlizzardIiPvE.CanUse(out act)) return true;
            if (BlizzardIiiPvE.CanUse(out act)) return true;
            if (BlizzardPvE.CanUse(out act)) return true;
        }
        return false;
    }

    private bool UsePolyglot(out IAction? act, uint gcdCount = 3)
    {
        act = null;

        if (gcdCount == 0 || IsPolyglotStacksMaxed && EnochianEndAfterGCD(gcdCount))
        {
            if (FoulPvE.CanUse(out act)) return true;
            if (XenoglossyPvE.CanUse(out act)) return true;
        }
        return false;
    }

    private bool MaintainStatus(out IAction? act)
    {
        act = null;
        if (CombatElapsedLess(6)) return false;
        if (UmbralSoulPvE.CanUse(out act)) return true;
        if (InAstralFire && TransposePvE.CanUse(out act)) return true;

        return false;
    }

    private bool NeedToGoIce
    {
        get
        {
            //Can use Despair.
            if (DespairPvE.EnoughLevel && CurrentMp >= DespairPvE.Info.MPNeed) return false;

            //Can use Fire1
            if (FirePvE.EnoughLevel && CurrentMp >= FirePvE.Info.MPNeed) return false;

            return true;
        }
    }

    private bool NeedToTransposeGoIce(bool usedOne)
    {
        if (!NeedToGoIce) return false;
        if (!ParadoxPvE.EnoughLevel) return false;
        var compare = usedOne ? -1 : 0;
        var count = PolyglotStacks;
        if (count == compare++) return false;
        if (count == compare++ && !EnochianEndAfterGCD(2)) return false;
        if (count >= compare && (HasFire || SwiftcastPvE.Cooldown.WillHaveOneChargeGCD(2) || TriplecastPvE.Cooldown.WillHaveOneChargeGCD(2))) return true;
        if (!HasFire && !SwiftcastPvE.Cooldown.WillHaveOneChargeGCD(2) && !TriplecastPvE.CanUse(out _, gcdCountForAbility: 8)) return false;
        return true;
    }

    #endregion
}
