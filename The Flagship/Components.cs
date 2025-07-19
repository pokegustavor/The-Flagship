using PulsarModLoader.Content.Components.MegaTurret;
using PulsarModLoader.Content.Components.Hull;
using UnityEngine;

namespace The_Flagship
{
    internal class Components
    {
        class FlagShipMainTurretMod : MegaTurretMod
        {
            public override string Name => "TheFlagship_FlagShipMainTurret";

            public override PLShipComponent PLMegaTurret => new FlagShipMainTurret();
        }
        class FlagShipHullMod : HullMod
        {
            public override string Name => "W.D Flagship Hull";

            public override string Description => "An extremely dense hull designed to support the massive scale of the W.D. Flagship, due to it's size this hull is only compatiable with the flagship.";

            public override int MarketPrice => 70000;

            public override bool CanBeDroppedOnShipDeath => false;

            public override float HullMax => 4000f;

            public override float Armor => 2.2f;
        }
    }
    class FlagShipMainTurret : PLMegaTurret
    {
        public FlagShipMainTurret(int inLevel = 0) : base(inLevel)
        {
            Level = inLevel;
            SubType = MegaTurretModManager.Instance.GetMegaTurretIDFromName("TheFlagship_FlagShipMainTurret");
            Name = "The Hullrender";
            Desc = "This monster of a turret will melt the hull of most ships that becomes it's target, but at the price of extemely slow charge and high power usage";
            BeamColor = Color.green;
            m_Damage = 1700f;
            TurretRange = 50000f;
            m_MaxPowerUsage_Watts = 25000f;
            FireDelay = 25f;
            m_MarketPrice = 29000;
            HeatGeneratedOnFire = 0.5f;
            CoolingRateModifier *= 0.5f;
            HasPulseLaser = false;
            HasTrackingMissileCapability = false;
            CanBeDroppedOnShipDeath = false;
        }
        protected override string GetTurretPrefabPath()
        {
            return "NetworkPrefabs/Component_Prefabs/CorruptedLaserTurret";
        }
        public override void Tick()
        {
            base.Tick();
            m_MaxPowerUsage_Watts = 25000f * LevelMultiplier(0.2f, 1f);
            if (TurretInstance != null && TurretInstance.OptionalGameObjects[1] != null)
            {
                bool flag = ChargeAmount > 0.7f;
                GameObject gameObject = TurretInstance.OptionalGameObjects[1];
                if (!IsFiring && flag)
                {
                    Ray ray = new Ray(TurretInstance.FiringLoc.position, TurretInstance.FiringLoc.forward);
                    RaycastHit raycastHit = default(RaycastHit);
                    int layerMask = 524289;
                    if (Physics.SphereCast(ray, 1f, out raycastHit, 20000f, layerMask))
                    {
                        LaserDist = (raycastHit.point - TurretInstance.FiringLoc.position).magnitude * (1f / TurretInstance.transform.parent.lossyScale.x);
                    }
                    else
                    {
                        LaserDist = 20000f;
                    }
                }
                if (gameObject != null)
                {
                    float num = Mathf.Min(50000f, LaserDist);
                    gameObject.transform.localPosition = new Vector3(0f, 0f, num * 0.5f);
                    Mathf.Abs(0.2f);
                    gameObject.transform.localScale = new Vector3(1f, num * 0.5f, 1f);
                    if (gameObject.activeSelf != flag)
                    {
                        gameObject.SetActive(flag);
                    }
                }
            }
        }
    }
}
