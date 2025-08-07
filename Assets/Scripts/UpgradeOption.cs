// FULL CODE FOR UpgradeOption.cs
using UnityEngine;

// Enum to specify which player stat this upgrade affects
public enum PlayerStatType
{
    DamageMultiplier,
    FireRate,
    MovementSpeed,
    Luck,
    MaxHealth 
}

// CreateAssetMenu allows you to create these assets via Right-Click -> Create -> Upgrades -> Upgrade Option
[CreateAssetMenu(fileName = "NewUpgradeOption", menuName = "Upgrades/Upgrade Option")]
public class UpgradeOption : ScriptableObject
{
    [Header("Upgrade Display Information")]
    [Tooltip("The quirky business pitch for this upgrade.")]
    [TextArea(3, 5)] // Makes the string field a multi-line text area in the Inspector
    public string promptDescription;

    // REMOVED: [Tooltip("The icon associated with this upgrade.")]
    // REMOVED: public Sprite icon;

    [Header("Stat Boost")]
    [Tooltip("Which player stat this upgrade will affect.")]
    public PlayerStatType statType;
    [Tooltip("The amount to add to the stat (0.5 for +, 1 for ++, 2 for +++).")]
    public float statBoostAmount;

    // Optional: A unique ID or name for this upgrade, useful for tracking if needed
    public string upgradeID;
}