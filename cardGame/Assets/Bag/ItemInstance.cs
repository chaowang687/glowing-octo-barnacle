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

        // 移除旋转相关属性，直接使用原始尺寸
        public int CurrentWidth => data.width;
        public int CurrentHeight => data.height;

        public ItemInstance(ItemData data) {
            this.data = data;
        }
    }

   
}