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
        public int rotation = 0; // 0, 90, 180, 270度

        // 根据旋转状态返回当前尺寸
        public int CurrentWidth => rotation == 90 || rotation == 270 ? data.height : data.width; // 旋转后宽高互换
        public int CurrentHeight => rotation == 90 || rotation == 270 ? data.width : data.height; // 旋转后宽高互换

        public ItemInstance(ItemData data) {
            this.data = data;
            this.rotation = 0;
        }
        
        /// <summary>
        /// 获取旋转后的实际形状
        /// </summary>
        public bool[,] GetActualShape() {
            // 获取形状数据
            bool[,] fullShape = data.shapeData.GetShapeArray();
            
            // 获取实际有效尺寸（使用ItemData的width和height）
            int w = data.width;
            int h = data.height;
            bool[,] originalShape = new bool[w, h];
            
            // 复制有效区域的形状数据
            for (int x = 0; x < w; x++) {
                for (int y = 0; y < h; y++) {
                    originalShape[x, y] = fullShape[x, y];
                }
            }
            
            // 根据旋转角度计算旋转后的尺寸
            int rotatedW = rotation == 90 || rotation == 270 ? h : w;
            int rotatedH = rotation == 90 || rotation == 270 ? w : h;
            
            // 二维数组维度顺序：[width, height]，即 [x轴大小, y轴大小]
            bool[,] rotatedShape = new bool[rotatedW, rotatedH];
            
            // 初始化旋转后的形状数组为false
            for (int i = 0; i < rotatedW; i++) {
                for (int j = 0; j < rotatedH; j++) {
                    rotatedShape[i, j] = false;
                }
            }
            
            switch (rotation) {
                case 0: // 0度 - 原样复制
                    for (int x = 0; x < w; x++) {
                        for (int y = 0; y < h; y++) {
                            if (x < rotatedW && y < rotatedH) {
                                rotatedShape[x, y] = originalShape[x, y];
                            }
                        }
                    }
                    break;
                    
                case 90: // 90度顺时针旋转：(x,y) → (y, rotatedH - 1 - x)
                    for (int x = 0; x < w; x++) {
                        for (int y = 0; y < h; y++) {
                            int newX = y;
                            int newY = rotatedH - 1 - x;
                            if (newX < rotatedW && newY < rotatedH) {
                                rotatedShape[newX, newY] = originalShape[x, y];
                            }
                        }
                    }
                    break;
                    
                case 180: // 180度旋转：(x,y) → (rotatedW - 1 - x, rotatedH - 1 - y)
                    for (int x = 0; x < w; x++) {
                        for (int y = 0; y < h; y++) {
                            int newX = rotatedW - 1 - x;
                            int newY = rotatedH - 1 - y;
                            if (newX < rotatedW && newY < rotatedH) {
                                rotatedShape[newX, newY] = originalShape[x, y];
                            }
                        }
                    }
                    break;
                    
                case 270: // 270度顺时针旋转：(x,y) → (rotatedW - 1 - y, x)
                    for (int x = 0; x < w; x++) {
                        for (int y = 0; y < h; y++) {
                            int newX = rotatedW - 1 - y;
                            int newY = x;
                            if (newX < rotatedW && newY < rotatedH) {
                                rotatedShape[newX, newY] = originalShape[x, y];
                            }
                        }
                    }
                    break;
            }
            
            return rotatedShape;
        }
        
        /// <summary>
        /// 旋转物品（顺时针90度）
        /// </summary>
        public void Rotate() {
            rotation = (rotation + 90) % 360;
        }
        
        /// <summary>
        /// 获取旋转后的星星绝对坐标
        /// </summary>
        public List<Vector2Int> GetStarPositions() {
            List<Vector2Int> starPositions = new List<Vector2Int>();
            
            // 获取物品当前宽高
            int currentWidth = CurrentWidth;
            int currentHeight = CurrentHeight;
            
            foreach (Vector2Int offset in data.starOffsets) {
                Vector2Int rotatedOffset = offset;
                
                // 根据旋转角度调整偏移量，使用与物品形状一致的旋转逻辑
                switch (rotation) {
                    case 90: // 90度顺时针旋转
                        rotatedOffset = new Vector2Int(offset.y, currentHeight - 1 - offset.x);
                        break;
                    case 180: // 180度旋转
                        rotatedOffset = new Vector2Int(currentWidth - 1 - offset.x, currentHeight - 1 - offset.y);
                        break;
                    case 270: // 270度顺时针旋转
                        rotatedOffset = new Vector2Int(currentWidth - 1 - offset.y, offset.x);
                        break;
                }
                
                // 计算绝对坐标
                starPositions.Add(new Vector2Int(posX + rotatedOffset.x, posY + rotatedOffset.y));
            }
            return starPositions;
        }
    }

   
}