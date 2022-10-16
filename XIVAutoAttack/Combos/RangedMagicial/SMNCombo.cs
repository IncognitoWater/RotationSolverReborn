using Dalamud.Game.ClientState.JobGauge.Types;
using System.Collections.Generic;
using XIVAutoAttack.Actions;
using XIVAutoAttack.Combos.CustomCombo;
using XIVAutoAttack.Configuration;

namespace XIVAutoAttack.Combos.RangedMagicial;

internal class SMNCombo : JobGaugeCombo<SMNGauge>
{
    public class SMNAction : BaseAction
    {
        internal override int Cast100 => new BaseAction(Service.IconReplacer.OriginalHook(ID)).Cast100;
        public SMNAction(uint actionID, bool isFriendly = false, bool shouldEndSpecial = false)
            : base(actionID, isFriendly, shouldEndSpecial)
        {

        }
    }
    internal override uint JobID => 27;
    protected override bool CanHealSingleSpell => false;
    private protected override BaseAction Raise => Actions.Resurrection;

    private static bool InBahamut => Service.IconReplacer.OriginalHook(25822) == Actions.Deathflare.ID;
    private static bool InPhoenix => Service.IconReplacer.OriginalHook(25822) == Actions.Rekindle.ID;
    private static bool InBreak => InBahamut || InPhoenix || Service.ClientState.LocalPlayer.Level < Actions.SummonBahamut.Level;
    internal struct Actions
    {
        public static readonly SMNAction
            //��ʯҫ
            Gemshine = new (25883)
            {
                OtherCheck = b => JobGauge.Attunement > 0,
            },

            //��ʯ��
            PreciousBrilliance = new (25884)
            {
                OtherCheck = b => JobGauge.Attunement > 0,
            },

            //���� ���幥��
            Ruin = new (163),

            //���� ��Χ�˺�
            Outburst = new (16511);

        public static readonly BaseAction
            //��ʯ���ٻ�
            SummonCarbuncle = new (25798)
            {
                OtherCheck = b => !TargetHelper.HavePet,
            },

            //����֮�� �Ÿ�
            SearingLight = new (25801)
            {
                OtherCheck = b => TargetHelper.InBattle && !InBahamut && !InPhoenix
                //&& JobGauge.ReturnSummon == Dalamud.Game.ClientState.JobGauge.Enums.SummonPet.NONE,
            },

            //�ػ�֮�� ���Լ�����
            RadiantAegis = new (25799),

            //ҽ��
            Physick = new (16230, true),

            //��̫���� 
            Aethercharge = new (25800)
            {
                OtherCheck = b => TargetHelper.InBattle,
            },

            //�����ٻ�
            SummonBahamut = new (7427),

            //�챦ʯ�ٻ�
            SummonRuby = new (25802)
            {
                OtherCheck = b => JobGauge.IsIfritReady && !XIVAutoAttackPlugin.movingController.IsMoving,
            },

            //�Ʊ�ʯ�ٻ�
            SummonTopaz = new (25803)
            {
                OtherCheck = b => JobGauge.IsTitanReady,
            },

            //�̱�ʯ�ٻ�
            SummonEmerald = new (25804)
            {
                OtherCheck = b => JobGauge.IsGarudaReady,
            },


            //����
            Resurrection = new (173, true),

            //��������
            EnergyDrain = new (16508),

            //������ȡ
            EnergySiphon = new (16510),

            //���ñ���
            Fester = new (181),

            //ʹ��˱�
            Painflare = new (3578),

            //�پ�
            RuinIV = new (7426)
            {
                BuffsNeed = new [] { ObjectStatus.FurtherRuin },
            },

            //����ŷ�
            EnkindleBahamut = new (7429)
            {
                OtherCheck = b => InBahamut || InPhoenix,
            },

            //���Ǻ˱�
            Deathflare = new (3582)
            {
                OtherCheck = b => InBahamut,
            },

            //����֮��
            Rekindle = new (25830, true)
            {
                OtherCheck = b => InPhoenix,
            },

            //�������
            CrimsonCyclone = new (25835)
            {
                BuffsNeed = new [] { ObjectStatus.IfritsFavor },
            },

            //���ǿϮ
            CrimsonStrike = new (25885),

            //ɽ��
            MountainBuster = new (25836)
            {
                BuffsNeed = new [] { ObjectStatus.TitansFavor },
            },

            //��������
            Slipstream = new (25837)
            {
                BuffsNeed = new [] { ObjectStatus.GarudasFavor },
            };
    }

    internal override SortedList<DescType, string> Description => new ()
    {
        {DescType.�������, $"{Actions.RadiantAegis.Action.Name}"},
        {DescType.��������, $"{Actions.Physick.Action.Name}"},
    };

    private protected override bool BreakAbility(byte abilityRemain, out IAction act)
    {
        //����֮��
        if (Actions.SearingLight.ShouldUseAction(out act, mustUse: true)) return true;

        return false;
    }

    private protected override bool MoveGCD(uint lastComboActionID, out IAction act)
    {
        if (Actions.CrimsonCyclone.ShouldUseAction(out act, mustUse: true)) return true;
        return base.MoveGCD(lastComboActionID, out act);
    }

    private protected override bool GeneralGCD(uint lastComboActionID, out IAction act)
    {
        //��ʯ���ٻ�
        if (Actions.SummonCarbuncle.ShouldUseAction(out act)) return true;

        //����
        if (!InBahamut && !InPhoenix)
        {
            if (Actions.RuinIV.ShouldUseAction(out act, mustUse: true)) return true;
            if (Actions.CrimsonStrike.ShouldUseAction(out act, lastComboActionID, mustUse: true)) return true;
            if (Actions.CrimsonCyclone.ShouldUseAction(out act, mustUse: true))
            {
                if (TargetFilter.DistanceToPlayer(Actions.CrimsonCyclone.Target) < 2)
                {
                    return true;
                }
            }
            if (Actions.Slipstream.ShouldUseAction(out act, mustUse: true)) return true;
        }


        //�ٻ�
        if (JobGauge.Attunement == 0)
        {
            if (Actions.SummonBahamut.ShouldUseAction(out act))
            {
                if (Actions.SearingLight.IsCoolDown || Service.ClientState.LocalPlayer.Level < Actions.SearingLight.Level)
                    return true;
            }
            else if (Actions.Aethercharge.ShouldUseAction(out act)) return true;

            if (JobGauge.IsIfritReady && JobGauge.IsGarudaReady && JobGauge.IsTitanReady ? JobGauge.SummonTimerRemaining == 0 : true)
            {
                switch (Config.GetComboByName("SummonOrder"))
                {
                    default:
                        //�� ��
                        if (Actions.SummonRuby.ShouldUseAction(out act)) return true;
                        //�� ��
                        if (Actions.SummonTopaz.ShouldUseAction(out act)) return true;
                        //�� ��
                        if (Actions.SummonEmerald.ShouldUseAction(out act)) return true;
                        break;
                    case 1:
                        //�� ��
                        if (Actions.SummonRuby.ShouldUseAction(out act)) return true;
                        //�� ��
                        if (Actions.SummonEmerald.ShouldUseAction(out act)) return true;
                        //�� ��
                        if (Actions.SummonTopaz.ShouldUseAction(out act)) return true;
                        break;
                    case 2:
                        //�� ��
                        if (Actions.SummonTopaz.ShouldUseAction(out act)) return true;
                        //�� ��
                        if (Actions.SummonEmerald.ShouldUseAction(out act)) return true;
                        //�� ��
                        if (Actions.SummonRuby.ShouldUseAction(out act)) return true;
                        break;
                    case 3:
                        //�� ��
                        if (Actions.SummonTopaz.ShouldUseAction(out act)) return true;
                        //�� ��
                        if (Actions.SummonRuby.ShouldUseAction(out act)) return true;
                        //�� ��
                        if (Actions.SummonEmerald.ShouldUseAction(out act)) return true;
                        break;
                    case 4:
                        //�� ��
                        if (Actions.SummonEmerald.ShouldUseAction(out act)) return true;
                        //�� ��
                        if (Actions.SummonRuby.ShouldUseAction(out act)) return true;
                        //�� ��
                        if (Actions.SummonTopaz.ShouldUseAction(out act)) return true;
                        break;
                    case 5:
                        //�� ��
                        if (Actions.SummonEmerald.ShouldUseAction(out act)) return true;
                        //�� ��
                        if (Actions.SummonTopaz.ShouldUseAction(out act)) return true;
                        //�� ��
                        if (Actions.SummonRuby.ShouldUseAction(out act)) return true;
                        break;
                }
            }
        }

        //AOE
        if (Actions.PreciousBrilliance.ShouldUseAction(out act)) return true;
        if (Actions.Outburst.ShouldUseAction(out act)) return true;

        //����
        if (Actions.Gemshine.ShouldUseAction(out act)) return true;
        if (Actions.Ruin.ShouldUseAction(out act)) return true;
        return false;
    }
    private protected override ActionConfiguration CreateConfiguration()
    {
        return base.CreateConfiguration().SetCombo("SummonOrder", 0, new string[]
        {
            "��-��-��", "��-��-��", "��-��-��", "��-��-��", "��-��-��", "��-��-��",

        }, "�����ٻ�˳��");
    }
    private protected override bool ForAttachAbility(byte abilityRemain, out IAction act)
    {
        if (Actions.EnkindleBahamut.ShouldUseAction(out act, mustUse: true)) return true;
        if (Actions.Deathflare.ShouldUseAction(out act, mustUse: true)) return true;
        if (Actions.Rekindle.ShouldUseAction(out act, mustUse: true)) return true;
        if (Actions.MountainBuster.ShouldUseAction(out act, mustUse: true)) return true;


        //��������
        if (JobGauge.HasAetherflowStacks && InBreak)
        {
            if (Actions.Painflare.ShouldUseAction(out act)) return true;
            if (Actions.Fester.ShouldUseAction(out act)) return true;
        }
        else
        {
            if (Actions.EnergySiphon.ShouldUseAction(out act)) return true;
            if (Actions.EnergyDrain.ShouldUseAction(out act)) return true;
        }

        return false;
    }

    private protected override bool DefenceSingleAbility(byte abilityRemain, out IAction act)
    {
        //�ػ�֮��
        if (Actions.RadiantAegis.ShouldUseAction(out act)) return true;

        return false;
    }

    private protected override bool HealSingleGCD(uint lastComboActionID, out IAction act)
    {
        //ҽ��
        if (Actions.Physick.ShouldUseAction(out act)) return true;

        return false;
    }

    private protected override bool DefenceAreaAbility(byte abilityRemain, out IAction act)
    {
        //����
        if (GeneralActions.Addle.ShouldUseAction(out act)) return true;
        return false;
    }
}
