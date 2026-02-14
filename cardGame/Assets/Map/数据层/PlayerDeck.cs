// PlayerDeck.cs
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PlayerDeck
{
    public List<string> cardIds = new List<string>();
    public int maxSize = 20;
    public int currentSize = 0;
    
    public PlayerDeck()
    {
        // 默认卡组
        cardIds = new List<string> { "Strike", "Defend", "Strike", "Defend", "Strike" };
        currentSize = cardIds.Count;
    }
    
    public bool AddCard(string cardId)
    {
        if (currentSize < maxSize)
        {
            cardIds.Add(cardId);
            currentSize++;
            return true;
        }
        Debug.LogWarning("卡组已满，无法添加新卡牌");
        return false;
    }
    
    public bool RemoveCard(string cardId)
    {
        if (cardIds.Remove(cardId))
        {
            currentSize--;
            return true;
        }
        return false;
    }
    
    public List<string> GetCards()
    {
        return new List<string>(cardIds);
    }
}