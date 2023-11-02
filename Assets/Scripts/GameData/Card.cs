using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "PlayCard", menuName = "Data/Create a new card")]
public class Card : ScriptableObject
{
    public int value;
    public string cardName;
    public string cardType;
    public string cardDescription;
    public Sprite cardSprite;
}
