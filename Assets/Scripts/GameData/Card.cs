using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "PlayCard", menuName = "Data/Create a new card")]
public class Card : ScriptableObject
{
    public enum CardType
    {
        PhysicalDamage,
        MagicalDamage,
        PhysicalBlock,
        MagicalBlock
    }

    public string cardName;
    public string cardDescription;
    public CardType cardType;
    public int value;
    public Sprite cardSprite;
}
