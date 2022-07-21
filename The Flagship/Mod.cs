using System.Collections.Generic;
using PulsarModLoader;
using PulsarModLoader.Chat.Commands.CommandRouter;
using UnityEngine;
using HarmonyLib;
using UnityEngine.SceneManagement;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
namespace The_Flagship
{
    public class Mod : PulsarMod
    {
        public override string Version => "1.0";

        public override string Author => "pokegustavo";

        public override string ShortDescription => "Adds the flagship as a playable ship.";

        public override string Name => "The Flagship";

        public override string HarmonyIdentifier()
        {
            return "pokegustavo.theflagship";
        }
    }

    public class testCommand : ChatCommand
    {
        public override string[] CommandAliases()
        {
            return new string[]
            {
                "test"
            };
        }

        public override string Description()
        {
            return "Test command";
        }

        public override void Execute(string arguments)
        {
            FabricateFlagship();
        }

        public void MoveObjAndChild(Transform transform, int layer)
        {
            foreach (Transform child in transform)
            {
                MoveObjAndChild(child, layer);
            }
            transform.gameObject.layer = layer;
            if (transform.gameObject.name.ToLower().Contains("ff_infected_mesh") || transform.gameObject.name.ToLower().Contains("if_stem") || transform.gameObject.name.ToLower().Contains("infected_bark") || transform.gameObject.tag == "lava" || transform.gameObject.name.ToLower().Contains("ff_infected_small")
                || transform.gameObject.name.ToLower().Contains("if_mound") || transform.gameObject.name.ToLower().Contains("infected_pool") || transform.gameObject.name.ToLower().Contains("terrain") || transform.gameObject.name.ToLower().Contains("infectedasset") || transform.gameObject.name.ToLower().Contains("infected_deco")
                || transform.gameObject.name.ToLower().Contains("infected_tunnel") || transform.gameObject.name.ToLower().Contains("infected_buddle") || transform.gameObject.name.ToLower().Contains("if_bark") || transform.gameObject.name.ToLower().Contains("missionnpc") || transform.gameObject.name.ToLower().Contains("captainchair")
                || transform.gameObject.name.ToLower().Contains("infectedsurface") || transform.gameObject.name.ToLower().Contains("missionvolume") || transform.gameObject.name.ToLower().Contains("if_splatter") || transform.gameObject.name.ToLower().Contains("if_bubble") || transform.gameObject.name.ToLower().Contains("fs_rot")
                || transform.gameObject.name.ToLower().Contains("spawns") || transform.gameObject.name.ToLower().Contains("snowps") || transform.gameObject.name.ToLower().Contains("particle system") || transform.gameObject.name.Split(' ')[0] == "Plane" || transform.gameObject.name.ToLower().Contains("rot_")
                || transform.gameObject.name.ToLower().Contains("humanoid_drone") || transform.gameObject.name.ToLower().Contains("generic_civ") || transform.gameObject.name.ToLower().Contains("deadbody") || transform.gameObject.name.ToLower().Contains("veins_") || transform.gameObject.name.ToLower().Contains("estate_4")
                || transform.gameObject.name.ToLower().Contains("exosuitasset") || transform.gameObject.name.ToLower().Contains("computer_good"))
            {
                transform.gameObject.tag = "Projectile";
            }
            if (transform.name.ToLower().Contains("light"))
            {
                transform.gameObject.SetActive(true);
            }
        }

        async void FabricateFlagship()
        {
            AsyncOperation op = SceneManager.LoadSceneAsync(67, LoadSceneMode.Additive);
            AsyncOperation op2 = SceneManager.LoadSceneAsync(105, LoadSceneMode.Additive);
            while (!op.isDone || !op2.isDone)
            {
                await Task.Yield();
            }
            Scene Estate = SceneManager.GetSceneByBuildIndex(67);
            Scene Flagship = SceneManager.GetSceneByBuildIndex(105);
            PLShipInfo ship = PLEncounterManager.Instance.PlayerShip;
            PLPilotingSystem currentpisys = ship.PilotingSystem;
            GameObject currentexterior = ship.Exterior;
            if (ship != null)
            {
                GameObject exterior = null;
                foreach (GameObject gameObject in Object.FindObjectsOfType<GameObject>())
                {
                    if (gameObject.name == "Test_Exterior")
                    {
                        exterior = gameObject;
                        break;
                    }
                }
                if (exterior != null)
                {
                    GameObject newexterior = Object.Instantiate(exterior, currentexterior.transform.position, exterior.transform.rotation);
                    newexterior.AddComponent<PLPilotingSystem>();
                    newexterior.GetComponent<PLPilotingSystem>().MyShipInfo = ship;
                    newexterior.GetComponent<PLPilotingSystem>().ActivationPoint = currentpisys.ActivationPoint;
                    newexterior.GetComponent<PLPilotingSystem>().ActivationPoint.position = new Vector3(-0.7f, -261f, -317.3f);
                    currentpisys.enabled = false;
                    currentpisys.ActivationPoint = null;
                    ship.PilotingSystem = newexterior.GetComponent<PLPilotingSystem>();
                    newexterior.AddComponent<PLPilotingHUD>();
                    newexterior.GetComponent<PLPilotingHUD>().MyShipInfo = ship;
                    ship.PilotingHUD = newexterior.GetComponent<PLPilotingHUD>();
                    newexterior.AddComponent<PLShipControl>();
                    newexterior.GetComponent<PLShipControl>().ShipInfo = ship;
                    newexterior.GetComponent<PLShipControl>().LatestSolution = new List<PathNode>();
                    ship.MyShipControl = newexterior.GetComponent<PLShipControl>();
                    newexterior.AddComponent<Rigidbody>();
                    newexterior.GetComponent<Rigidbody>().useGravity = false;
                    newexterior.GetComponent<Rigidbody>().angularDrag = 1.1f;
                    newexterior.GetComponent<Rigidbody>().drag = 0.37f;
                    newexterior.GetComponent<Rigidbody>().inertiaTensor = new Vector3(14139.08f, 22628.87f, 13388.7f);
                    newexterior.GetComponent<Rigidbody>().inertiaTensorRotation = new Quaternion(359.9627f, -0.0003f, -0.0001f, 0);
                    newexterior.GetComponent<Rigidbody>().interpolation = RigidbodyInterpolation.Interpolate;
                    newexterior.GetComponent<Rigidbody>().mass = 680;
                    newexterior.GetComponent<PLShipControl>()._rigidbody = newexterior.GetComponent<Rigidbody>();
                    newexterior.transform.SetParent(ship.ShipRoot.transform);
                    newexterior.layer = currentexterior.layer;
                    newexterior.AddComponent<PLFlightAI>();
                    newexterior.GetComponent<PLFlightAI>().MyShipInfo = ship;
                    newexterior.tag = "Ship";
                    Object.DontDestroyOnLoad(newexterior);
                    ship.ExteriorRenderers.Add(newexterior.GetComponent<MeshRenderer>());
                    ship.ExteriorRigidbody = newexterior.GetComponent<Rigidbody>();
                    ship.ExteriorMeshRenderer = newexterior.GetComponent<MeshRenderer>();
                    ship.Exterior = newexterior;
                    ship.ExteriorCollider = newexterior.GetComponent<MeshCollider>();
                    ship.ExteriorMeshCollider = newexterior.GetComponent<MeshCollider>();
                    ship.Hull_Virtual_MeshCollider = newexterior.GetComponent<MeshCollider>();
                    ship.OrbitCameraMinDistance = 920f;
                    ship.OrbitCameraMaxDistance = 1550f;
                    ship.WarpBlocker.transform.SetParent(newexterior.transform);
                    ship.WarpObject.transform.SetParent(newexterior.transform);
                    ship.WarpObject.transform.localPosition = Vector3.zero;
                    ship.WarpBlocker.transform.localPosition = Vector3.zero;
                    ship.MainTurretPoint.transform.SetParent(newexterior.transform);
                    ship.MainTurretPoint.transform.localPosition = new Vector3(0, 171.563f, -478.4174f);
                    ship.MainTurretPoint.transform.localScale = Vector3.one * 10f;
                    ship.MainTurretPoint.transform.localRotation = new Quaternion(0, 0, 0, 0);
                    ship.RegularTurretPoints[0].transform.SetParent(newexterior.transform);
                    ship.RegularTurretPoints[0].transform.localPosition = new Vector3(-75, 40.4253f, 235.0909f);
                    ship.RegularTurretPoints[0].transform.localScale = Vector3.one * 10f;
                    ship.RegularTurretPoints[0].transform.localRotation = new Quaternion(0, 0, 0, 0);
                    ship.RegularTurretPoints[1].transform.SetParent(newexterior.transform);
                    ship.RegularTurretPoints[1].transform.localPosition = new Vector3(75, 40.4253f, 235.0909f);
                    ship.RegularTurretPoints[1].transform.localScale = Vector3.one * 10f;
                    ship.RegularTurretPoints[1].transform.localRotation = new Quaternion(0, 0, 0, 0);
                    GameObject clamps = null;
                    foreach (GameObject gameObject in Object.FindObjectsOfType<GameObject>())
                    {
                        if (gameObject.name == "Exterior_FactoryClamps" && gameObject.transform.parent == newexterior.transform)
                        {
                            clamps = gameObject;
                            break;
                        }
                    }
                    if (clamps != null)
                    {
                        Object.Destroy(clamps);
                    }
                    foreach (MeshRenderer renderer in currentexterior.GetComponentsInChildren<MeshRenderer>())
                    {
                        renderer.enabled = false;
                    }
                    foreach (MeshCollider collider in currentexterior.GetComponentsInChildren<MeshCollider>())
                    {
                        collider.enabled = false;
                    }
                    currentexterior.transform.position = newexterior.transform.position;
                    currentexterior.transform.SetParent(newexterior.transform);
                    currentexterior.SetActive(false);
                }
                GameObject interior = null;
                GameObject bridge = null;
                GameObject rightwing = null;
                GameObject rightwingDeco = null;
                GameObject vault = null;
                GameObject vaultDeco = null;
                GameObject engineering = null;
                GameObject reactorroom = null;
                GameObject copydoor = null;
                GameObject smallturret1 = null;
                GameObject smallturret2 = null;
                GameObject mainturret = null;
                GameObject weaponssys = null;
                GameObject nukeswitch1 = null;
                GameObject nukeswitch2 = null;
                GameObject nukecore = null;
                GameObject lifesys = null;
                GameObject sciencesys = null;
                GameObject fuelboard = null;
                GameObject fueldecal = null;
                GameObject metalblock = null;
                foreach (GameObject gameObject in Object.FindObjectsOfType<GameObject>(true))
                {
                    if (gameObject.name == "Planet")
                    {
                        interior = gameObject;
                    }
                    else if (gameObject.name == "BridgeBSO")
                    {
                        bridge = gameObject;
                    }
                    else if (gameObject.name == "FS_RightCrew_01")
                    {
                        rightwing = gameObject;
                    }
                    else if (gameObject.name == "RIGHT_CREW")
                    {
                        rightwingDeco = gameObject;
                    }
                    else if (gameObject.name == "FS_Vault")
                    {
                        vault = gameObject;
                    }
                    else if (gameObject.name == "VAULT")
                    {
                        vaultDeco = gameObject;
                    }
                    else if (gameObject.name == "FS_Engineering_Room")
                    {
                        engineering = gameObject.transform.parent.gameObject;
                    }
                    else if (gameObject.name == "REACTOR" && gameObject.scene.buildIndex == 67)
                    {
                        reactorroom = gameObject;
                    }
                    else if (gameObject.name == "WD_AutomatedDoors (3)")
                    {
                        copydoor = gameObject;
                    }
                    else if(gameObject.name == "Turrett_Control_01" && smallturret1 == null && gameObject != smallturret2) 
                    {
                        smallturret1 = gameObject;
                    }
                    else if (gameObject.name == "Turrett_Control_01" && gameObject != smallturret1)
                    {
                        smallturret2 = gameObject;
                    }
                    else if (gameObject.name == "Turrett_Control_02")
                    {
                        mainturret = gameObject;
                    }
                    else if(gameObject.name == "SystemInstance_Weapons") 
                    {
                        weaponssys = gameObject;
                    }
                    else if(gameObject.name == "Switch_01" && gameObject.transform.childCount > 0 && gameObject.transform.GetChild(0).name == "pCube24" && nukeswitch1 == null && gameObject != nukeswitch2 && gameObject.transform.childCount == 3)
                    {
                        nukeswitch1 = gameObject;
                    }
                    else if (gameObject.name == "Switch_01" && gameObject.transform.childCount > 0 && gameObject.transform.GetChild(0).name == "pCube24" && gameObject != nukeswitch1 && gameObject.transform.childCount == 3)
                    {
                        nukeswitch2 = gameObject;
                    }
                    else if(gameObject.name == "Nuke_Desk_02") 
                    {
                        nukecore = gameObject;
                    }
                    else if(gameObject.name == "LifeSupportSysInstance") 
                    {
                        lifesys = gameObject;
                    }
                    else if(gameObject.name == "ComputerSysInstance") 
                    {
                        sciencesys = gameObject;
                    }
                    else if (gameObject.name == "FuelBoard")
                    {
                        fuelboard = gameObject;
                    }
                    else if (gameObject.name == "Decal_ManualProgramCharge")
                    {
                        fueldecal = gameObject;
                    }
                    else if (gameObject.name == "MetalBlock_01")
                    {
                        metalblock = gameObject;
                    }
                    if (interior != null && bridge != null && rightwing != null && rightwingDeco != null && vault != null && vaultDeco != null && engineering != null && reactorroom != null && copydoor != null
                        && smallturret1 != null && smallturret2 != null && mainturret != null && weaponssys != null && nukeswitch1 != null && nukeswitch2 != null && nukecore != null && lifesys != null
                        && sciencesys != null && fuelboard != null && fueldecal != null && metalblock != null) break;
                }
                if (interior != null && bridge != null && rightwing != null && rightwingDeco != null && vault != null && vaultDeco != null && engineering != null)
                {
                    GameObject newinterior = Object.Instantiate(interior, interior.transform.position + new Vector3(400, -400, 1500), interior.transform.rotation);
                    GameObject newbridge = Object.Instantiate(bridge, new Vector3(0, -258.2285f, -409.0034f), bridge.transform.rotation);
                    GameObject newrighwing = Object.Instantiate(rightwing, new Vector3(357.7801f, -367.354f, 1681.315f), rightwing.transform.rotation);
                    GameObject newrightwingdeco = Object.Instantiate(rightwingDeco, new Vector3(462.3824f, -431.5907f, 1618.373f), rightwingDeco.transform.rotation);
                    Vector3 offset = (newrighwing.transform.position - rightwing.transform.position);
                    GameObject newvault = Object.Instantiate(vault, vault.transform.position + offset, vault.transform.rotation);
                    GameObject newvaultdeco = Object.Instantiate(vaultDeco, vaultDeco.transform.position + offset, vaultDeco.transform.rotation);
                    GameObject newengineering = Object.Instantiate(engineering, engineering.transform.position + offset + new Vector3(0, 0, 2), new Quaternion(0, 0.0029f, 0, 1));
                    GameObject newreactor = Object.Instantiate(reactorroom, reactorroom.transform.position + offset, new Quaternion(0, 0.0029f, 0, 1));
                    newbridge.transform.localRotation = new Quaternion(0.2202f, 0.0157f, 0.0263f, -0.975f);
                    Object.DontDestroyOnLoad(newinterior);
                    Object.DontDestroyOnLoad(newbridge);
                    Object.DontDestroyOnLoad(newrighwing);
                    Object.DontDestroyOnLoad(newrightwingdeco);
                    Object.DontDestroyOnLoad(newvault);
                    Object.DontDestroyOnLoad(newvaultdeco);
                    Object.DontDestroyOnLoad(newengineering);
                    Object.DontDestroyOnLoad(newreactor);
                    GameObject oldEngineDoor = null;
                    foreach (GameObject @object in Object.FindObjectsOfType<GameObject>(true))
                    {
                        if (@object.name == "WD_AutomatedDoors (14)" && @object.transform.parent.parent == newinterior.transform)
                        {
                            oldEngineDoor = @object;
                            break;
                        }
                    }
                    if (oldEngineDoor != null)
                    {
                        GameObject newEngineDoor = Object.Instantiate(copydoor, oldEngineDoor.transform.position, oldEngineDoor.transform.rotation);
                        newEngineDoor.transform.SetParent(newinterior.transform);
                        oldEngineDoor.transform.position += new Vector3(0, 20, 0);

                    }
                    foreach (Transform transform in newinterior.transform)
                    {
                        if (transform.gameObject.name == "REACTOR")
                        {
                            transform.gameObject.tag = "Projectile";
                            break;
                        }
                    }
                    MoveObjAndChild(newinterior.transform, ship.InteriorStatic.layer);
                    MoveObjAndChild(newbridge.transform, ship.InteriorStatic.layer);
                    MoveObjAndChild(newrighwing.transform, ship.InteriorStatic.layer);
                    MoveObjAndChild(newrightwingdeco.transform, ship.InteriorStatic.layer);
                    MoveObjAndChild(newvault.transform, ship.InteriorStatic.layer);
                    MoveObjAndChild(newvaultdeco.transform, ship.InteriorStatic.layer);
                    MoveObjAndChild(newengineering.transform, ship.InteriorStatic.layer);
                    MoveObjAndChild(newreactor.transform, ship.InteriorStatic.layer);
                    ship.InteriorStatic.transform.localPosition = ship.InteriorStatic.transform.localPosition + new Vector3(0, 0, 1500);
                    newrightwingdeco.transform.SetParent(newrighwing.transform);
                    newrighwing.transform.SetParent(newinterior.transform);
                    newvault.transform.SetParent(newinterior.transform);
                    newvaultdeco.transform.SetParent(newvault.transform);
                    newengineering.transform.SetParent(newinterior.transform);
                    newreactor.transform.SetParent(newinterior.transform);
                    GameObject[] infectedstuff = GameObject.FindGameObjectsWithTag("Projectile");
                    foreach (GameObject @object in infectedstuff)
                    {
                        Object.DestroyImmediate(@object);
                    }
                    foreach (MeshRenderer render in newinterior.GetComponentsInChildren<MeshRenderer>(true))
                    {
                        if (render != null)
                        {
                            ship.InteriorRenderers.Add(render);
                        }
                    }
                    foreach (MeshRenderer render in newbridge.GetComponentsInChildren<MeshRenderer>(true))
                    {
                        if (render != null)
                        {
                            ship.InteriorRenderers.Add(render);
                        }
                    }
                    ship.InteriorShipLights.Clear();
                    foreach (Light light in newbridge.GetComponentsInChildren<Light>(true))
                    {
                        if (light != null)
                        {
                            ship.InteriorShipLights.Add(light);
                            light.enabled = true;
                        }
                    }
                    foreach (Light light in newinterior.GetComponentsInChildren<Light>(true))
                    {
                        if (light != null)
                        {
                            ship.InteriorShipLights.Add(light);
                            light.enabled = true;
                        }
                    }
                    foreach (PLDoor door in newinterior.GetComponentsInChildren<PLDoor>(true))
                    {
                        if (door != null)
                        {
                            door.gameObject.SetActive(true);
                        }
                    }
                    foreach (PLLockedSeamlessDoor lockedoor in newinterior.GetComponentsInChildren<PLLockedSeamlessDoor>(true))
                    {
                        if (lockedoor != null)
                        {
                            lockedoor.RequiredItem = "Hands";
                        }
                    }
                    foreach (PLKillVolume volume in newinterior.GetComponentsInChildren<PLKillVolume>())
                    {
                        volume.enabled = false;
                    }
                    PLTeleportationScreen origianlTP = null;
                    foreach (PLUIScreen mesa in ship.MyScreenBase.AllScreens)
                    {
                        if (mesa is PLTeleportationScreen)
                        {
                            origianlTP = mesa as PLTeleportationScreen;
                            break;
                        }
                    }
                    ship.CaptainsChairPivot.position = new Vector3(-0.7818f, -261.7534f, -324.4219f);
                    if (origianlTP != null)
                    {
                        foreach (PLClonedScreen screen in newinterior.GetComponentsInChildren<PLClonedScreen>())
                        {
                            screen.MyTargetScreen = origianlTP;
                        }
                    }
                    PLInteriorDoor CapitanToBridge = newinterior.GetComponentInChildren<PLInteriorDoor>(true);
                    PLInteriorDoor BridgeToCaptain = newbridge.GetComponentInChildren<PLInteriorDoor>(true);
                    if (CapitanToBridge != null && BridgeToCaptain != null)
                    {
                        CapitanToBridge.OptionalTLI = ship.MyTLI;
                        CapitanToBridge.TargetDoor = BridgeToCaptain;
                        BridgeToCaptain.TargetDoor = CapitanToBridge;
                        BridgeToCaptain.OptionalTLI = ship.MyTLI;
                        GameObject BridgeToEngineOjb = Object.Instantiate(BridgeToCaptain.gameObject, new Vector3(-0.6f, -261, -443.1f), new Quaternion(0, 0, 0, 1));
                        Object.DontDestroyOnLoad(BridgeToEngineOjb);
                        PLInteriorDoor BridgeToEngine = BridgeToEngineOjb.GetComponent<PLInteriorDoor>();
                        GameObject EngineToBridgeOjb = Object.Instantiate(BridgeToCaptain.gameObject, new Vector3(378.7f, -384.8f, 1366.8f), new Quaternion(0, 0, 0, 1));
                        Object.DontDestroyOnLoad(EngineToBridgeOjb);
                        PLInteriorDoor EngineToBridge = EngineToBridgeOjb.GetComponent<PLInteriorDoor>();
                        if(BridgeToEngine != null && EngineToBridge != null) 
                        {
                            BridgeToEngine.TargetDoor = EngineToBridge;
                            BridgeToEngine.VisibleName = "Engineering";
                            EngineToBridge.TargetDoor = BridgeToEngine;
                            EngineToBridge.VisibleName = "Bridge";
                        }
                    }
                    ship.AllInteriorShipLightIntensities = new float[ship.InteriorShipLights.Count];
                    ship.AllInteriorShipLightColors = new Color[ship.InteriorShipLights.Count];
                    ship.AllInteriorShipLightModIntensities = new float[ship.InteriorShipLights.Count];
                    int num2 = 0;
                    foreach (Light light3 in ship.InteriorShipLights)
                    {
                        light3.cullingMask = -4722690;
                        ship.AllInteriorShipLightIntensities[num2] = ship.InteriorShipLights[num2].intensity;
                        ship.AllInteriorShipLightColors[num2] = ship.InteriorShipLights[num2].color;
                        num2++;
                        if (light3.gameObject.activeSelf && light3.gameObject.GetComponent<PLDisableLight>() == null)
                        {
                            PLGameStatic.Instance.AddDLI(new DisableLightInfo
                            {
                                light = light3,
                                minQualityLevel = -1,
                                isOnShip = true
                            });
                        }
                    }
                    for (int j = 0; j < ship.InteriorShipLights.Count; j++)
                    {
                        ship.AllInteriorShipLightModIntensities[j] = 0f;
                    }
                    if (newinterior.GetComponent<MeshRenderer>() != null) ship.InteriorRenderers.Add(newinterior.GetComponent<MeshRenderer>());
                    PLSpawner[] spawners = newinterior.GetComponentsInChildren<PLSpawner>();
                    foreach (PLSpawner spawner in newinterior.GetComponentsInChildren<PLSpawner>())
                    {
                        spawner.enabled = false;
                    }
                    for (int i = 0; i < spawners.Length; i++)
                    {
                        Object.Destroy(spawners[i]);
                    }
                    newinterior.AddComponent<PLShipConnection>();
                    newinterior.GetComponent<PLShipConnection>().MyShip = ship;
                    foreach (PLTeleportationLocationInstance tp in Object.FindObjectsOfType<PLTeleportationLocationInstance>())
                    {
                        if (tp.name == "PLGame" && tp.AllTTIs.Length > 1 && tp.AllTTIs[1].gameObject.transform.parent == newinterior.transform)
                        {
                            //tp.MyShipInfo = ship;
                            //ship.MyTLI = tp;
                            break;
                        }
                    }
                    ship.InteriorStatic.transform.position = new Vector3(367.3f,-382.3f,1548);
                    newinterior.transform.SetParent(ship.InteriorStatic.transform);
                    newbridge.transform.SetParent(ship.InteriorStatic.transform);
                    if (smallturret1 != null) 
                    {
                        smallturret1.transform.position = new Vector3(390.0232f, -383.5964f, 1517.274f);
                        smallturret1.transform.rotation = new Quaternion(0, 0, 0, 1);
                        ship.InteriorRenderers.Add(smallturret1.GetComponent<MeshRenderer>());
                    }
                    if (smallturret2 != null)
                    {
                        smallturret2.transform.position = new Vector3(326.2234f, - 383.5964f, 1517.274f);
                        smallturret2.transform.rotation = new Quaternion(0, 1, 0, 0);
                        ship.InteriorRenderers.Add(smallturret2.GetComponent<MeshRenderer>());
                    }
                    if(mainturret != null) 
                    {
                        mainturret.transform.position = new Vector3(358, - 383.6652f, 1572.374f);
                        mainturret.transform.rotation = new Quaternion(0, 0.7071f, 0, -0.7071f);
                        ship.InteriorRenderers.Add(mainturret.GetComponent<MeshRenderer>());
                    }
                    if(weaponssys != null) 
                    {
                        weaponssys.transform.position = new Vector3(358.1818f, - 383.6548f, 1588.564f);
                        weaponssys.transform.rotation = new Quaternion(0,0.7071f,0,-0.7071f);
                        ship.SysInstUIRoots[1].transform.position = new Vector3(358.1926f, -382.2694f, 1587.644f);
                        ship.SysInstUIRoots[1].transform.rotation = new Quaternion(0, 0, 0, 1);
                    }
                    if(nukecore != null && nukeswitch1 != null && nukeswitch2 != null) 
                    {
                        nukeswitch1.transform.SetParent(nukecore.transform);
                        nukeswitch2.transform.SetParent(nukecore.transform);
                        nukeswitch2.transform.localPosition = new Vector3(0.3883f, 1.5048f, -0.873f);
                        ship.NukeActivator.transform.SetParent(nukecore.transform);
                        nukecore.transform.position = new Vector3(371.977f, -383.6779f, 1609);
                        nukecore.transform.rotation = new Quaternion(0,1,0,0);
                    }
                    if(lifesys != null) 
                    {
                        lifesys.transform.position = new Vector3(380.4184f, - 400.8968f, 1732.827f);
                        lifesys.transform.rotation = new Quaternion(0,1,0,0);
                        ship.SysInstUIRoots[3].transform.position = new Vector3(381.32f, - 399.8868f, 1732.855f);
                        ship.SysInstUIRoots[3].transform.rotation = new Quaternion(0, 0.7071f, 0, -0.7071f);
                    }
                    if(sciencesys != null) 
                    {
                        sciencesys.transform.position = new Vector3(335.9909f, - 400.8744f, 1732.785f);
                        sciencesys.transform.rotation = new Quaternion(-0.002f, 0, 0.0005f, -1);
                        ship.SysInstUIRoots[0].transform.position = new Vector3(335.433f,- 399.7129f, 1732.852f);
                        ship.SysInstUIRoots[0].transform.rotation = new Quaternion(0.0014f, -0.7071f, -0.0043f, -0.7071f);
                        ship.ResearchWorldRootBGObj.transform.position = new Vector3(316.6057f, -399, 1735.064f);
                        ship.ResearchWorldRootBGObj.transform.rotation = new Quaternion(0, 0.7071f, 0, -0.7071f);
                        ship.ResearchLockerWorldRootBGObj.transform.position = new Vector3(315.9459f, -398.9975f, 1732.498f);
                        ship.ResearchLockerWorldRootBGObj.transform.rotation = new Quaternion(0, 0.4468f, 0, -0.8946f);
                        ship.ResearchLockerFrame.transform.position = new Vector3(316.0803f, - 399.738f, 1732.706f);
                        ship.ResearchLockerFrame.transform.rotation = new Quaternion(0, 0.4468f, 0, -0.8946f);
                        ship.ResearchLockerCollider.transform.position = new Vector3(316.0803f, -399.738f, 1732.706f);
                        ship.ResearchLockerCollider.transform.rotation = new Quaternion(0, 0.4468f, 0, -0.8946f);
                        ship.ResearchLockerWorldRoot.transform.position = new Vector3(316.0803f, -399.738f, 1732.706f);
                        ship.ResearchLockerWorldRoot.transform.rotation = new Quaternion(0, 0.4468f, 0, -0.8946f);
                    }
                    if(fuelboard != null) 
                    {
                        Vector3 original = ship.WarpFuelBoostLever.transform.position;
                        Quaternion originalrot = ship.WarpFuelBoostLever.transform.rotation;
                        ship.WarpFuelBoostLever.transform.position = new Vector3(372.5025f, - 348.0554f, 1391.55f);
                        ship.WarpFuelBoostLever.transform.rotation = new Quaternion(0,0.997f,0,0.0773f);
                        fuelboard.transform.position += ship.WarpFuelBoostLever.transform.position - original;
                        fuelboard.transform.rotation = ship.WarpFuelBoostLever.transform.rotation;
                        if(fueldecal != null) 
                        {
                            fueldecal.transform.position += ship.WarpFuelBoostLever.transform.position - original;
                            fueldecal.transform.rotation = ship.WarpFuelBoostLever.transform.rotation;
                        }
                        if(metalblock != null) 
                        {
                            metalblock.transform.position += ship.WarpFuelBoostLever.transform.position - original;
                            metalblock.transform.rotation = ship.WarpFuelBoostLever.transform.rotation;
                        }
                    }
                    PLTeleportationScreen tpscreen = null;
                    PLScientistSensorScreen sensorscreen = null;
                    PLScientistComputerScreen computerscreen = null;
                    PLScientistVirusScreen virusscreen = null;
                    PLCommsScreen commsScreen = null;
                    PLCaptainScreen statusscreen = null;
                    PLEngineerReactorScreen reactorscreen = null;
                    PLWarpDriveScreen warpscreen = null;
                    PLEngineerCoolantScreen coolantscreen = null;
                    PLEngineerAuxReactorScreen auxscreen = null;
                    PLUIPilotingScreen pilotscren = null;
                    PLClonedScreen clonedScreen = null;
                    PLMissileLauncherScreen misslescreen = null;
                    PLWeaponsNukeScreen nukescreen = null;
                    foreach (PLUIScreen screen in ship.MyScreenBase.AllScreens)
                    {
                        if (screen is PLTeleportationScreen)
                        {
                            tpscreen = screen as PLTeleportationScreen;
                        }
                        else if (screen is PLScientistSensorScreen)
                        {
                            sensorscreen = screen as PLScientistSensorScreen;
                        }
                        else if (screen is PLScientistComputerScreen)
                        {
                            computerscreen = screen as PLScientistComputerScreen;
                        }
                        else if (screen is PLScientistVirusScreen)
                        {
                            virusscreen = screen as PLScientistVirusScreen;
                        }
                        else if (screen is PLCommsScreen)
                        {
                            commsScreen = screen as PLCommsScreen;
                        }
                        else if (screen is PLCaptainScreen)
                        {
                            statusscreen = screen as PLCaptainScreen;
                        }
                        else if (screen is PLEngineerReactorScreen)
                        {
                            reactorscreen = screen as PLEngineerReactorScreen;
                        }
                        else if (screen is PLWarpDriveScreen)
                        {
                            warpscreen = screen as PLWarpDriveScreen;
                        }
                        else if (screen is PLEngineerCoolantScreen)
                        {
                            coolantscreen = screen as PLEngineerCoolantScreen;
                        }
                        else if (screen is PLEngineerAuxReactorScreen)
                        {
                            auxscreen = screen as PLEngineerAuxReactorScreen;
                        }
                        else if (screen is PLUIPilotingScreen)
                        {
                            pilotscren = screen as PLUIPilotingScreen;
                        }
                        else if (screen is PLClonedScreen && screen.name == "ClonedScreen_Status")
                        {
                            clonedScreen = screen as PLClonedScreen;
                        }
                        else if(screen is PLMissileLauncherScreen) 
                        {
                            misslescreen = screen as PLMissileLauncherScreen;
                        }
                        else if(screen is PLWeaponsNukeScreen) 
                        {
                            nukescreen = screen as PLWeaponsNukeScreen;
                        }
                        if (screen.transform.childCount > 2)
                        {
                            Transform fisical = screen.transform.GetChild(0);
                            Object.Destroy(fisical.gameObject);
                        }
                    }
                    if (tpscreen != null)
                    {
                        tpscreen.gameObject.transform.position = new Vector3(427.7254f, -429.5034f, 1729.931f);
                        tpscreen.gameObject.transform.rotation = new Quaternion(0, 0.7071f, 0, -0.7071f);
                        if (clonedScreen != null)
                        {
                            GameObject teleport1 = Object.Instantiate(clonedScreen.gameObject, new Vector3(357.3508f, -441.1491f, 1481.12f), new Quaternion(0, 0.9239f, 0, -0.3827f));
                            Object.DontDestroyOnLoad(teleport1);
                            teleport1.transform.SetParent(ship.InteriorDynamic.transform);
                            ship.MyScreenBase.AllScreens.Add(teleport1.GetComponent<PLClonedScreen>());
                            teleport1.GetComponent<PLClonedScreen>().MyTargetScreen = tpscreen;
                            GameObject teleport2 = Object.Instantiate(clonedScreen.gameObject, new Vector3(349.7148f, -441.5712f, 1663.779f), new Quaternion(0, 0.8434f, 0, -0.5373f));
                            Object.DontDestroyOnLoad(teleport2);
                            teleport2.transform.SetParent(ship.InteriorDynamic.transform);
                            ship.MyScreenBase.AllScreens.Add(teleport2.GetComponent<PLClonedScreen>());
                            teleport2.GetComponent<PLClonedScreen>().MyTargetScreen = tpscreen;
                        }
                    }
                    if (sensorscreen != null)
                    {
                        sensorscreen.transform.position = new Vector3(-1.1564f, -260.2763f, -321.4911f);
                        sensorscreen.transform.rotation = new Quaternion(0, 0.998f, 0, 0.0635f);
                        if(clonedScreen != null) 
                        {
                            GameObject teleport1 = Object.Instantiate(clonedScreen.gameObject, new Vector3(336.5857f, - 399.4763f, 1730.905f), new Quaternion(0, 0.7071f, 0, -0.7071f));
                            Object.DontDestroyOnLoad(teleport1);
                            teleport1.transform.SetParent(ship.InteriorDynamic.transform);
                            ship.MyScreenBase.AllScreens.Add(teleport1.GetComponent<PLClonedScreen>());
                            teleport1.GetComponent<PLClonedScreen>().MyTargetScreen = sensorscreen;
                        }
                    }
                    if (computerscreen != null)
                    {
                        computerscreen.transform.position = new Vector3(-0.1764f, -260.2763f, -321.4911f);
                        computerscreen.transform.rotation = new Quaternion(0, 0.9971f, 0, -0.0755f);
                        if (clonedScreen != null)
                        {
                            GameObject teleport1 = Object.Instantiate(clonedScreen.gameObject, new Vector3(336.5857f, -399.4763f, 1729.965f), new Quaternion(0, 0.7071f, 0, -0.7071f));
                            Object.DontDestroyOnLoad(teleport1);
                            teleport1.transform.SetParent(ship.InteriorDynamic.transform);
                            ship.MyScreenBase.AllScreens.Add(teleport1.GetComponent<PLClonedScreen>());
                            teleport1.GetComponent<PLClonedScreen>().MyTargetScreen = computerscreen;
                        }
                    }
                    if (virusscreen != null)
                    {
                        virusscreen.transform.position = new Vector3(0.8726f, -260.2763f, -321.88f);
                        virusscreen.transform.rotation = new Quaternion(0, 0.9632f, 0, -0.2686f);
                        if (clonedScreen != null)
                        {
                            GameObject teleport1 = Object.Instantiate(clonedScreen.gameObject, new Vector3(336.5857f, -399.4763f, 1729.01f), new Quaternion(0, 0.7071f, 0, -0.7071f));
                            Object.DontDestroyOnLoad(teleport1);
                            teleport1.transform.SetParent(ship.InteriorDynamic.transform);
                            ship.MyScreenBase.AllScreens.Add(teleport1.GetComponent<PLClonedScreen>());
                            teleport1.GetComponent<PLClonedScreen>().MyTargetScreen = virusscreen;
                        }
                    }
                    if (commsScreen != null)
                    {
                        commsScreen.transform.position = new Vector3(-2.1074f, -260.276f, -321.7779f);
                        commsScreen.transform.rotation = new Quaternion(0, 0.9728f, 0, 0.2318f);
                    }
                    if (statusscreen != null)
                    {
                        statusscreen.transform.position = new Vector3(-3.0238f, -260.276f, -322.3781f);
                        statusscreen.transform.rotation = new Quaternion(0, 0.9065f, 0, 0.4222f);
                    }
                    if (reactorscreen != null)
                    {
                        reactorscreen.transform.position = new Vector3(-0.3147f, -260.276f, -327.1871f);
                        reactorscreen.transform.rotation = new Quaternion(0, 0.0776f, 0, -0.997f);
                        if (clonedScreen != null)
                        {
                            GameObject teleport1 = Object.Instantiate(clonedScreen.gameObject, new Vector3(361.3329f, -383.2109f, 1373.86f), new Quaternion(0, 0.3907f, 0, 0.9205f));
                            Object.DontDestroyOnLoad(teleport1);
                            teleport1.transform.SetParent(ship.InteriorDynamic.transform);
                            ship.MyScreenBase.AllScreens.Add(teleport1.GetComponent<PLClonedScreen>());
                            teleport1.GetComponent<PLClonedScreen>().MyTargetScreen = reactorscreen;
                        }
                    }
                    if (warpscreen != null)
                    {
                        warpscreen.transform.position = new Vector3(-1.3783f, -260.276f, -327.1472f);
                        warpscreen.transform.rotation = new Quaternion(0, 0.1093f, 0, 0.994f);
                        if (clonedScreen != null)
                        {
                            GameObject teleport1 = Object.Instantiate(clonedScreen.gameObject, new Vector3(356.2941f, -383.2109f, 1374.809f), new Quaternion(0, 0.1313f, 0, -0.9913f));
                            Object.DontDestroyOnLoad(teleport1);
                            teleport1.transform.SetParent(ship.InteriorDynamic.transform);
                            ship.MyScreenBase.AllScreens.Add(teleport1.GetComponent<PLClonedScreen>());
                            teleport1.GetComponent<PLClonedScreen>().MyTargetScreen = warpscreen;
                        }
                    }
                    if (coolantscreen != null)
                    {
                        coolantscreen.transform.position = new Vector3(0.6817f, -260.276f, -326.9217f);
                        coolantscreen.transform.rotation = new Quaternion(0, 0.2374f, 0, -0.9714f);
                        if (clonedScreen != null)
                        {
                            GameObject teleport1 = Object.Instantiate(clonedScreen.gameObject, new Vector3(358.9745f, -383.2109f, 1375.063f), new Quaternion(0, 0.1502f, 0, 0.9887f));
                            Object.DontDestroyOnLoad(teleport1);
                            teleport1.transform.SetParent(ship.InteriorDynamic.transform);
                            ship.MyScreenBase.AllScreens.Add(teleport1.GetComponent<PLClonedScreen>());
                            teleport1.GetComponent<PLClonedScreen>().MyTargetScreen = coolantscreen;
                        }
                    }
                    if (auxscreen != null)
                    {
                        auxscreen.transform.position = new Vector3(-2.2474f, -260.276f, -326.8127f);
                        auxscreen.transform.rotation = new Quaternion(0, 0.2286f, 0, 0.9735f);
                        if (clonedScreen != null)
                        {
                            GameObject teleport1 = Object.Instantiate(clonedScreen.gameObject, new Vector3(362.6146f, - 383.2109f, 1371.575f), new Quaternion(0, 0.6088f, 0, 0.7934f));
                            Object.DontDestroyOnLoad(teleport1);
                            teleport1.transform.SetParent(ship.InteriorDynamic.transform);
                            ship.MyScreenBase.AllScreens.Add(teleport1.GetComponent<PLClonedScreen>());
                            teleport1.GetComponent<PLClonedScreen>().MyTargetScreen = auxscreen;
                        }
                    }
                    if (pilotscren != null)
                    {
                        pilotscren.transform.position = new Vector3(-1.5436f, -260.6036f, -316.256f);
                    }
                    if(clonedScreen != null) 
                    {
                        clonedScreen.transform.position = new Vector3(0.0273f, -260.6036f, -316.256f);
                        clonedScreen.transform.rotation = new Quaternion(0, 0.9239f, 0, -0.3827f);
                    }
                    if (misslescreen != null)
                    {
                        misslescreen.transform.position = new Vector3(359.1238f, - 382.3235f, 1572.909f);
                        misslescreen.transform.rotation = new Quaternion(0, 0.3827f, 0, -0.9239f);
                    }
                    if(nukescreen != null) 
                    {
                        nukescreen.transform.position = new Vector3(371.3765f, - 382.3674f, 1610.65f);
                        nukescreen.transform.rotation = new Quaternion(0, 0.9239f, 0, -0.3827f);
                    }
                    ship.DialogueChoiceBG.transform.position = new Vector3(0.7823f, - 259, - 321.5387f);
                    ship.DialogueChoiceBG.transform.rotation = new Quaternion(-0.1016f,0.1994f,0.0202f,0.9744f);
                    ship.DialogueTextBG.transform.position = new Vector3(-2.7849f, -259, -321.5387f);
                    ship.DialogueTextBG.transform.rotation = new Quaternion(0.0861f, 0.2357f, 0.0244f, -0.9672f);
                    ship.MyTLI.AllTTIs[0].transform.position = new Vector3(425.1f, -430, 1729.7f);
                    GameObject newtp = Object.Instantiate(ship.MyTLI.AllTTIs[0].gameObject, new Vector3(355.8f, -441.5f, 1479.2f), ship.MyTLI.AllTTIs[0].transform.rotation);
                    GameObject newtp2 = Object.Instantiate(ship.MyTLI.AllTTIs[0].gameObject, new Vector3(347.5f, -442f, 1662.4f), ship.MyTLI.AllTTIs[0].transform.rotation);
                    Object.DontDestroyOnLoad(newtp);
                    Object.DontDestroyOnLoad(newtp2);
                    PLTeleportationTargetInstance[] newtargets = new PLTeleportationTargetInstance[3];
                    newtargets[0] = ship.MyTLI.AllTTIs[0];
                    newtargets[1] = newtp.GetComponent<PLTeleportationTargetInstance>();
                    newtargets[2] = newtp2.GetComponent<PLTeleportationTargetInstance>();
                    newtargets[0].TeleporterTargetName = "Capitan Room";
                    newtargets[1].TeleporterTargetName = "Reactor";
                    newtargets[2].TeleporterTargetName = "Bar";
                    ship.MyTLI.AllTTIs = newtargets;
                    ship.ExosuitVisualAssets[0].transform.position = new Vector3(388.613f, - 400.052f, 1724.208f);
                    ship.ExosuitVisualAssets[0].transform.rotation = new Quaternion(0,0.7377f,0,-0.6752f);
                    ship.ExosuitVisualAssets[1].transform.position = new Vector3(389.513f, -400.052f, 1724.208f);
                    ship.ExosuitVisualAssets[1].transform.rotation = new Quaternion(0, 0.7377f, 0, -0.6752f);
                    ship.ExosuitVisualAssets[2].transform.position = new Vector3(390.213f, -400.052f, 1724.208f);
                    ship.ExosuitVisualAssets[2].transform.rotation = new Quaternion(0, 0.7377f, 0, -0.6752f);
                    ship.ExosuitVisualAssets[3].transform.position = new Vector3(390.913f, -400.052f, 1724.208f);
                    ship.ExosuitVisualAssets[3].transform.rotation = new Quaternion(0, 0.7377f, 0, -0.6752f);
                    ship.ExosuitVisualAssets[4].transform.position = new Vector3(391.613f, -400.052f, 1724.208f);
                    ship.ExosuitVisualAssets[4].transform.rotation = new Quaternion(0, 0.7377f, 0, -0.6752f);
                    ship.MyAtrium.transform.position = new Vector3(380.1347f, -399.7f, 1723f);
                    GameObject newatrium = Object.Instantiate(ship.MyAtrium.gameObject, new Vector3(380.1347f, -399.7f, 1727f), ship.MyAtrium.transform.rotation);
                    Object.DontDestroyOnLoad(newatrium.transform);
                    newatrium.transform.SetParent(newinterior.transform);
                    newatrium = Object.Instantiate(ship.MyAtrium.gameObject, new Vector3(380.1347f, -399.7f, 1738f), ship.MyAtrium.transform.rotation);
                    Object.DontDestroyOnLoad(newatrium.transform);
                    newatrium.transform.SetParent(newinterior.transform);
                    newatrium = Object.Instantiate(ship.MyAtrium.gameObject, new Vector3(380.1347f, -399.7f, 1742f), ship.MyAtrium.transform.rotation);
                    Object.DontDestroyOnLoad(newatrium.transform);
                    newatrium.transform.SetParent(newinterior.transform);
                    //ship.InteriorStatic = interior;
                    PLNetworkManager.Instance.MyLocalPawn.transform.position = new Vector3(0, -258.2285f, -409.0034f);
                }
            }
            SceneManager.UnloadSceneAsync(Estate);
            SceneManager.UnloadSceneAsync(Flagship);
        }
    }

    [HarmonyPatch(typeof(PLShipInfo), "Update")]
    public class Update
    {
        static void Postfix(PLShipInfo __instance, Color ___WarpObjectCurColor)
        {
            if (__instance.WarpBlocker != null)
            {
                __instance.WarpBlocker.transform.localScale = Vector3.one * (12000f - ___WarpObjectCurColor.a * 6000f);
            }
            if (!__instance.ShowingExterior && __instance.GetIsPlayerShip() && (PLNetworkManager.Instance.MyLocalPawn == null || PLNetworkManager.Instance.MyLocalPawn.CurrentShip == __instance))
            {
                __instance.InteriorRenderers.RemoveAll((MeshRenderer render) => render == null);
                __instance.ExteriorMeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
                if (PLNetworkManager.Instance.LocalPlayer.GetPlayerID() == __instance.SensorDishControllerPlayerID) __instance.ExteriorMeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
                foreach (MeshRenderer render in __instance.InteriorRenderers)
                {
                    render.enabled = true;
                    render.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
                }
                foreach (Light light in __instance.InteriorShipLights)
                {
                    try
                    {
                        if (light.gameObject != null)
                        {
                            light.gameObject.SetActive(true);
                        }
                    }
                    catch { }
                }
            }
        }
    }
}
