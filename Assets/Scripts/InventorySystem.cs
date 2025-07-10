using UnityEngine;
using System.Collections.Generic;

public class InventorySystem : MonoBehaviour
{
    public Dictionary<string, int> inventory = new Dictionary<string, int>();
    public int maxSlots = 6; // �������� ���������
    private int currentSlots = 0;

    void Start()
    {
        // ������������� ���������
        inventory.Add("Battery", 0);
        inventory.Add("Medkit", 0);
        inventory.Add("Ammo", 0);
    }

    public bool AddItem(string itemName)
    {
        if (currentSlots >= maxSlots)
        {
            Debug.Log("Inventory full!");
            return false;
        }

        if (inventory.ContainsKey(itemName))
        {
            inventory[itemName]++;
            currentSlots++;
            Debug.Log($"Added {itemName}. Total: {inventory[itemName]}");
            return true;
        }
        return false;
    }

    public bool UseItem(string itemName)
    {
        if (inventory.ContainsKey(itemName) && inventory[itemName] > 0)
        {
            inventory[itemName]--;
            currentSlots--;
            Debug.Log($"Used {itemName}. Remaining: {inventory[itemName]}");
            return true;
        }
        Debug.Log($"No {itemName} in inventory!");
        return false;
    }

    // ������: ��������� ������� ��� ��������������
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position, transform.forward, out hit, 2f))
            {
                if (hit.collider.CompareTag("Pickup"))
                {
                    string itemName = hit.collider.gameObject.name; // ��������, "Battery"
                    if (AddItem(itemName))
                    {
                        Destroy(hit.collider.gameObject); // ������� ������� �� �����
                    }
                }
            }
        }

        // ������ ������������� ��������
        if (Input.GetKeyDown(KeyCode.Q))
        {
            UseItem("Battery");
        }
    }
}