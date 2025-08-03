

using UnityEngine;

namespace AncestralMod.Patches;

public class Knocker : MonoBehaviour
{
	protected Item? item;

	private void Awake()
	{
		item = GetComponent<Item>();
	}

	private void OnCollisionEnter(Collision collision)
	{

		if (item == null || item.holderCharacter != null || collision.collider == null)
		{
			return;	
		}

		Vector3 velocity = item.rig.linearVelocity;
		if (velocity.magnitude < 4.5f)
		{
			return;
		}

		Character characterCollided = collision.collider.GetComponentInParent<Character>();
		if (characterCollided != null && characterCollided.IsLocal && item.timeSinceWasActive > 0.1f)
		{
			velocity = collision.relativeVelocity;
			if (!(velocity.magnitude < ConfigHandler.KnockerMinVelocity.Value))
			{
				Debug.Log("ITEM KO! at velocity: " + velocity.magnitude);
				characterCollided.Fall(2f);
			}
		}
	}
}