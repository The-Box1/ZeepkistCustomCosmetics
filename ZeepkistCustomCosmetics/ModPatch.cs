using System;
using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace ZeepkistCustomCosmetics;

[HarmonyPatch(typeof(CosmeticWardrobe), "PopulateCompleteDictionaries")]
public class CosmeticWardrobePopulateCompleteDictionariesPatch
{
    static GameObject GetAlternateChildren(GameObject obj, params string[] alternatives)
    {
        foreach (string child in alternatives)
        {
            Transform childTransform = obj.transform.Find(child);
            if (childTransform) return childTransform.gameObject;
        }

        return null;
    }
    
    static void StoreZeepkists(CosmeticWardrobe wardrobe, string path)
    {
        string[] files = Directory.GetFiles(path);

        foreach (string file in files)
        {
            AssetBundle bundle = AssetBundle.LoadFromFile(file);

            GameObject[] gameObjects = bundle.LoadAllAssets<GameObject>();
            
            GameObject car = gameObjects[0];
            if (!car)
            {
                Main.Logger.LogError("Bundle had no assets");
                wardrobe.manager.messenger.LogError("Bundle had no assets", 5f);
                return;
            }

            try
            {
                GameObject soapboxTemplate = GetAlternateChildren(car, "Soapbox Template", "Soapbox_Template");
                GameObject auxObjects = GetAlternateChildren(car, "Aux Objects", "Aux_Objects");
                GameObject soapbox = GetAlternateChildren(car, "Soapbox Template/Soapbox", "Soapbox_Template/Soapbox");
                GameObject frontWheel = GetAlternateChildren(car, "Soapbox Template/Front Wheel", "Soapbox_Template/Front_Wheel");
                GameObject rearWheel = GetAlternateChildren(car, "Soapbox Template/Rear Wheel (optional)", "Soapbox_Template/Rear_Wheel__optional_");
                GameObject suspension = GetAlternateChildren(car, "Soapbox Template/Suspension", "Soapbox_Template/Suspension");
                
                if (
                    soapboxTemplate != null &&
                    auxObjects != null &&
                    soapbox != null &&
                    frontWheel != null &&
                    rearWheel != null &&
                    suspension != null
                )
                {
                    Object_Soapbox obj = car.AddComponent<Object_Soapbox>();

                    obj.soapboxModel = soapbox.GetComponent<MeshRenderer>();
                    obj.soapboxModelMesh = soapbox.GetComponent<MeshFilter>();
                    obj.wheelModelFront = frontWheel.GetComponent<MeshRenderer>();
                    obj.wheelModelFrontMesh = frontWheel.GetComponent<MeshFilter>();
                    obj.dontMirrorRightWheels = false;
                    obj.optionalWheelModelRear = rearWheel.GetComponent<MeshRenderer>();
                    obj.optionalWheelModelRearMesh = rearWheel.GetComponent<MeshFilter>();
                    obj.suspensionMaterialModel = suspension.GetComponent<MeshRenderer>();
                    obj.auxiliaryObjectsBase = auxObjects;

                    int nextId = wardrobe.zeepkistShelfs[^1].familyID + 1;
                    
                    GameObject shelfHolder = new GameObject(nextId * 1000 + " - " + car.name);
                    shelfHolder.transform.parent = wardrobe.gameObject.transform;
                    CosmeticShelf shelf = shelfHolder.AddComponent<CosmeticShelf>();
                    shelf.cosmeticType = CosmeticShelf.FamilyType.zeepkist;
                    shelf.cosmetics = new List<CosmeticItemBase> { obj };
                    shelf.familyID = nextId;

                    obj.itemID = 0;
                    obj.itemType = CosmeticShelf.FamilyType.zeepkist;
                    obj.typeOfLock = CosmeticItemBase.ProgressionLock.regular;
                    obj.parentShelf = shelf;
                    obj.alternateFamilyID = 0;
                    obj.overrideTestMode = false;
                    
                    wardrobe.zeepkistShelfs.Add(shelf);
                    wardrobe.everyZeepkist.Add(obj.GetCompleteID(), obj);
                    Main.newZeepkists.Add(obj.GetCompleteID());
                }
            }
            catch (Exception)
            {
                Main.Logger.LogError(car.name + ": Invalid object");
                wardrobe.manager.messenger.LogError(car.name + ": Invalid object", 5f);
            }
        }
    }
    
    static void StoreHats(CosmeticWardrobe wardrobe, string path)
    {
        string[] files = Directory.GetFiles(path);

        foreach (string file in files)
        {
            AssetBundle bundle = AssetBundle.LoadFromFile(file);

            GameObject[] gameObjects = bundle.LoadAllAssets<GameObject>();

            GameObject hat = gameObjects[0];
            if (!hat)
            {
                Main.Logger.LogError("Bundle had no assets");
                wardrobe.manager.messenger.LogError("Bundle had no assets", 5f);
                return;
            }

            try
            {
                GameObject hatRoot = hat.transform.Find("Hat").gameObject;
                
                if (
                    hatRoot != null
                )
                {
                    HatValues obj = hat.AddComponent<HatValues>();

                    obj.lockRenderer = hatRoot.GetComponent<MeshRenderer>();

                    int nextId = wardrobe.hatShelfs[^1].familyID + 1;
                    
                    GameObject shelfHolder = new GameObject(nextId * 1000 + " - " + hat.name);
                    shelfHolder.transform.parent = wardrobe.gameObject.transform;
                    CosmeticShelf shelf = shelfHolder.AddComponent<CosmeticShelf>();
                    shelf.cosmeticType = CosmeticShelf.FamilyType.hat;
                    shelf.cosmetics = new List<CosmeticItemBase> { obj };
                    shelf.familyID = nextId;

                    obj.itemID = 0;
                    obj.itemType = CosmeticShelf.FamilyType.hat;
                    obj.typeOfLock = CosmeticItemBase.ProgressionLock.regular;
                    obj.parentShelf = shelf;
                    obj.alternateFamilyID = 0;
                    obj.overrideTestMode = false;
                    
                    wardrobe.hatShelfs.Add(shelf);
                    wardrobe.everyHat.Add(obj.GetCompleteID(), obj);
                    
                    Main.newHats.Add(obj.GetCompleteID());
                }
            }
            catch (Exception)
            {
                Main.Logger.LogError(hat.name + ": Invalid object");
                wardrobe.manager.messenger.LogError(hat.name + ": Invalid object", 5f);
            }
        }
    }
    
    static void Postfix(CosmeticWardrobe __instance)
    {
        StoreZeepkists(__instance, Main.zeepkistPath);
        StoreHats(__instance, Main.hatPath);
    }
}

[HarmonyPatch(typeof(ProgressionMaestro), "GetCosmetics")]
public class ProgressionMaestroGetCosmeticsPatch
{
    static void Postfix(ProgressionMaestro __instance, ref List<int> __result, CosmeticShelf.FamilyType famType)
    {
        if (famType == CosmeticShelf.FamilyType.zeepkist) __result.AddRange(Main.newZeepkists);
        if (famType == CosmeticShelf.FamilyType.hat) __result.AddRange(Main.newHats);
    }
}

[HarmonyPatch(typeof(ProgressionMaestro), "SaveCosmetics")]
public class ProgressionMaestroSaveCosmeticsPatch
{
    static void Prefix(ProgressionMaestro __instance, int saveLine, ref List<int> cosmeticValues)
    {
        if (saveLine == 1) cosmeticValues.RemoveAll(i => Main.newZeepkists.Contains(i)); // Zeepkists
        if (saveLine == 2) cosmeticValues.RemoveAll(i => Main.newHats.Contains(i)); // Hats
    }
}

[HarmonyPatch(typeof(AvonturenKaartScript), "CloseGarage")]
public class AvonturenKaartScriptCloseGaragePatch
{
    static void Postfix(AvonturenKaartScript __instance)
    {
        if (!Main.newZeepkists.Contains(__instance.manager.avontuurSoapbox.GetCompleteID())) Main.lastSelectedGameZeepkist.Value = __instance.manager.avontuurSoapbox.GetCompleteID();
        if (!Main.newHats.Contains(__instance.manager.avontuurHat.GetCompleteID())) Main.lastSelectedGameHat.Value = __instance.manager.avontuurHat.GetCompleteID();
    }
}

[HarmonyPatch(typeof(NetworkedZeepkistGhost), "Awake")]
public class NetworkedZeepkistGhostAwakePatch
{
    static bool Prefix(NetworkedZeepkistGhost __instance)
    {
        __instance.bitValues = new bool[8];
        __instance.manager = GameObject.Find("Game Manager").GetComponent<PlayerManager>();
        __instance.wardrobe = __instance.manager.objectsList.wardrobe;
        __instance.transform.parent = __instance.manager.photonManager.spawner.transform;
        __instance.gameObject.name = __instance.view.Owner.NickName;

        int zeepkistId = __instance.manager.avontuurSoapbox.GetCompleteID();
        int hatId = __instance.manager.avontuurHat.GetCompleteID();

        if (Main.lastSelectedGameZeepkist.Value != -1) zeepkistId = Main.lastSelectedGameZeepkist.Value;
        if (Main.lastSelectedGameHat.Value != -1) hatId = Main.lastSelectedGameHat.Value;
        
        __instance.playerProperties.Add("Zeepkist", __instance.wardrobe.GetZeepkistUnlocked(zeepkistId).GetCompleteID());
        __instance.playerProperties.Add("Hat", __instance.wardrobe.GetHatUnlocked(hatId).GetCompleteID());
        
        __instance.playerProperties.Add("Color", __instance.wardrobe.GetColorUnlocked(__instance.manager.avontuurColor.GetCompleteID()).GetCompleteID());
        __instance.playerProperties.Add("SteamID", __instance.manager.steamAchiever.GetPlayerSteamID().ToString());
        PhotonNetwork.SetPlayerCustomProperties(__instance.playerProperties);
        
        return false;
    }
}

[HarmonyPatch(typeof(OnlineResultsPodium), "DoOnlineResultsPodium")]
public class OnlineResultsPodiumDoOnlineResultsPodium
{
    static int GetZeepkistId(OnlineResultsPodium instance, Player player)
    {
        if (player.IsLocal) return instance.manager.avontuurSoapbox.GetCompleteID();
        return (int)player.CustomProperties["Zeepkist"];
    }
    
    static int GetHatId(OnlineResultsPodium instance, Player player)
    {
        if (player.IsLocal) return instance.manager.avontuurHat.GetCompleteID();
        return (int)player.CustomProperties["Hat"];
    }
    
    static bool Prefix(OnlineResultsPodium __instance, Player[] leaderboard)
    {
        __instance.gameObject.SetActive(value: true);
		GameObject gameObject = GameObject.Find("355 - Podium");
		if (gameObject != null)
		{
            __instance.transform.position = gameObject.transform.position;
            __instance.transform.rotation = gameObject.transform.rotation;
            __instance.transform.localScale = gameObject.transform.localScale;
            __instance.localPodium.SetActive(value: false);
		}
		else
		{
            __instance.transform.position = new Vector3(0f, 500f, 0f);
		}
        __instance.includeThisTop3.Clear();
        __instance.includeThisTop3.Add(item: false);
        __instance.includeThisTop3.Add(item: false);
        __instance.includeThisTop3.Add(item: false);
        __instance.winner1.gameObject.SetActive(value: false);
        __instance.winner2.gameObject.SetActive(value: false);
        __instance.winner3.gameObject.SetActive(value: false);
        __instance.wintext1.text = "";
        __instance.wintext2.text = "";
        __instance.wintext3.text = "";
		for (int i = 0; i < 3; i++)
		{
			if (leaderboard.Length <= i)
			{
				continue;
			}
			object[] playerResults = __instance.manager.photonManager.GetPlayerResults(leaderboard[i]);
			if (playerResults.Length > 2)
			{
				string formattedTime = __instance.manager.GetFormattedTime((float)playerResults[1]);
				int iD = GetZeepkistId(__instance, leaderboard[i]);
				int iD2 = GetHatId(__instance, leaderboard[i]);
				int iD3 = (int)leaderboard[i].CustomProperties["Color"];
				switch (i)
				{
				case 0:
                    __instance.winner1.gameObject.SetActive(value: true);
                    __instance.winner1.DoCarSetup(__instance.wardrobe.GetZeepkist(iD), __instance.wardrobe.GetHat(iD2), __instance.wardrobe.GetColor(iD3), doLights: true);
                    __instance.wintext1.text = leaderboard[i].NickName.NoParse() + "\n" + formattedTime;
					break;
				case 1:
                    __instance.winner2.gameObject.SetActive(value: true);
                    __instance.winner2.DoCarSetup(__instance.wardrobe.GetZeepkist(iD), __instance.wardrobe.GetHat(iD2), __instance.wardrobe.GetColor(iD3), doLights: true);
                    __instance.wintext2.text = leaderboard[i].NickName.NoParse() + "\n" + formattedTime;
					break;
				default:
                    __instance.winner3.gameObject.SetActive(value: true);
                    __instance.winner3.DoCarSetup(__instance.wardrobe.GetZeepkist(iD), __instance.wardrobe.GetHat(iD2), __instance.wardrobe.GetColor(iD3), doLights: true);
                    __instance.wintext3.text = leaderboard[i].NickName.NoParse() + "\n" + formattedTime;
					break;
				}
			}
			else
			{
                __instance.includeThisTop3[i] = true;
			}
		}
        __instance.dropTheseGuys.Clear();
		for (int j = 0; j < leaderboard.Length; j++)
		{
            __instance.dropTheseGuys.Add(Object.Instantiate(__instance.fallingCarPrefab));
            __instance.dropTheseGuys[j].transform.localScale = Vector3.one;
			int iD4 = GetZeepkistId(__instance, leaderboard[j]);
			int iD5 = GetHatId(__instance, leaderboard[j]);
			int iD6 = (int)leaderboard[j].CustomProperties["Color"];
            __instance.dropTheseGuys[j].DoCarSetup(__instance.wardrobe.GetZeepkist(iD4), __instance.wardrobe.GetHat(iD5), __instance.wardrobe.GetColor(iD6), doLights: true);
            __instance.dropTheseGuys[j].transform.position = __instance.dropGuysHere.position + Random.insideUnitSphere * 8f;
            __instance.dropTheseGuys[j].transform.rotation = Random.rotation;
            __instance.dropTheseGuys[j].gameObject.SetActive(value: false);
			if (j < __instance.includeThisTop3.Count && !__instance.includeThisTop3[j])
			{
                __instance.dropTheseGuys[j].transform.position = new Vector3(0f, -1000f, 0f);
			}
		}

        return false;
    }
}