using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayDeck", menuName = "Data/Create a new deck")]
public class Deck : ScriptableObject
{
    public List<Card> cards;
}
