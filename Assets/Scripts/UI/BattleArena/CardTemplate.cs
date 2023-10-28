using System.Collections;
using System.Collections.Generic;
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

    // will create owner later after touching Network stuff

    // Setter for each individual card information
    public void InitializeCardInformation(string cardName_in, string cardValue_in, string cardDesc_in, Sprite cardSprite_in)
    {
        cardNameLabel.text = cardName_in;
        cardValueLabel.text = cardValue_in;
        cardDescLabel.text = cardDesc_in;
        cardImage.sprite = cardSprite_in;
    }
}
