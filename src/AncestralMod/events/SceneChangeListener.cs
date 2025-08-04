using UnityEngine;
using UnityEngine.SceneManagement;

namespace AncestralMod.Events;

public class SceneChangeListener
{

	public static void Initialize()
	{
		SceneManager.sceneLoaded += OnSceneLoaded;
	}

	private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		if (scene.name.ToLower() == "airport") {
			AirportCheckInKiosk checkinKiosk = Object.FindFirstObjectByType<AirportCheckInKiosk>();
			AirportInviteFriendsKiosk friendKiosk = Object.FindFirstObjectByType<AirportInviteFriendsKiosk>();

			checkinKiosk.transform.position = new Vector3(-11, 1.5f, 52.5f);
			checkinKiosk.transform.eulerAngles = new Vector3(270, 0, 0);

			friendKiosk.transform.position = new Vector3(-8, 1.5f, 52.5f);
			friendKiosk.transform.eulerAngles = new Vector3(270, 180, 0);
		}
	}

}
