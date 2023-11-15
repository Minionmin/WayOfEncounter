using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class CardTemplate : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI cardNameLabel;
    [SerializeField] private TextMeshProUGUI cardValueLabel;
    [SerializeField] private TextMeshProUGUI cardDescLabel;
    [SerializeField] private Image cardImage;
    [SerializeField] private Image cardBackImage;
    [SerializeField] private Transform cardValueTransform;
    private Card.CardType cardType;


    // Setter for each individual card information
    public void InitializeCardInformation(string cardName_in, string cardValue_in, string cardDesc_in, Sprite cardSprite_in, Card.CardType cardType_in)
    {
        cardNameLabel.text = cardName_in;
        cardValueLabel.text = cardValue_in;
        cardDescLabel.text = cardDesc_in;
        cardImage.sprite = cardSprite_in;
        cardType = cardType_in;
    }

    public Image GetCardBackImage()
    {
        return cardBackImage;
    }

    public Transform GetCardValueTransform()
    {
        return cardValueTransform;
    }

    public string GetCardName()
    {
        return cardNameLabel.text;
    }

    public int GetCardValue()
    {
        int cardValue = -1;
        int.TryParse(cardValueLabel.text, out cardValue);

        return cardValue;
    }

    public Card.CardType GetCardType()
    {
        return cardType;
    }
}
