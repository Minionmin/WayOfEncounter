using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "PlayCard", menuName = "Data/Create a new card")]
public class Card : ScriptableObject
{
    public string cardName;
    public string cardType;
    public Sprite cardSprite;
    public string cardDescription;
    public int value;
}
