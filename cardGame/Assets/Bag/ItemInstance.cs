using UnityEngine;
using System;
using System.Collections.Generic;

namespace Bag
{
    [Serializable]
    public class ItemInstance {
        public ItemData data;
        public int posX;
        public int posY;
        public bool isRotated;

        public int CurrentWidth => isRotated ? data.height : data.width;
        public int CurrentHeight => isRotated ? data.width : data.height;

        public ItemInstance(ItemData data) {
            this.data = data;
        }
    }

   
}