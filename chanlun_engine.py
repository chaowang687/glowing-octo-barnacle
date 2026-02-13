#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
缠论量化引擎（优化版）
来源：https://github.com/...
功能：K线包含处理、分型、笔、线段、中枢、买卖点识别
"""

import numpy as np
import pandas as pd
from typing import List, Tuple, Optional


class ChanQuantEngine:
    """
    缠论量化交易引擎（优化版）
    包含：K线包含处理、分型、笔、线段、中枢、买卖点识别
    支持回测信号输出
    """
    def __init__(self, 
                 bi_threshold: float = 0.001,       # 笔的最低价格变动比例（相对于价格）
                 max_include_len: int = 5,          # 最多连续包含处理次数
                 use_macd: bool = True,              # 是否使用MACD背驰
                 macd_fast: int = 12,
                 macd_slow: int = 26,
                 macd_signal: int = 9):
        self.bi_threshold = bi_threshold
        self.max_include_len = max_include_len
        self.use_macd = use_macd
        self.macd_fast = macd_fast
        self.macd_slow = macd_slow
        self.macd_signal = macd_signal
        
        # 存储计算结果
        self.k_merged = None          # 合并后的K线 DataFrame
        self.fractals = []            # 分型列表 [(idx, type, high, low)]
        self.bi = []                  # 笔列表 [(start_idx, end_idx, direction, start_price, end_price)]
        self.segments = []             # 线段列表（可选）
        self.zhongshu = []             # 中枢列表 [(start_idx, end_idx, zg, zd)]
        self.signals = pd.DataFrame()  # 最终信号表
        
    def merge_k_lines(self, df: pd.DataFrame) -> pd.DataFrame:
        """
        处理K线包含关系，返回合并后的K线序列
        df 必须包含 'high','low','open','close'，索引为datetime
        返回的DataFrame包含合并后的K线，并保留原始索引范围
        """
        data = df[['high','low','open','close']].copy()
        data = data.reset_index().rename(columns={'index':'date'})
        # 按时间排序
        data = data.sort_values('date')
        
        merged = []
        i = 0
        n = len(data)
        while i < n:
            if i == 0:
                # 第一根K线直接加入
                merged.append(data.iloc[i].to_dict())
                i += 1
                continue
            
            current = data.iloc[i]
            prev = merged[-1]
            
            # 检查包含关系：当前K线高点 <= 前一根高点 且 当前低点 >= 前一根低点 -> 被包含
            # 或者 当前高点 >= 前一根高点 且 当前低点 <= 前一根低点 -> 包含前一根
            if current['high'] <= prev['high'] and current['low'] >= prev['low']:
                # 当前被前一根包含：需要合并到前一根
                # 方向判断：如果前一根是上升（close>open），则取高点更高，低点更高；下降则取低点更低，高点更低
                if prev['close'] >= prev['open']:  # 阳线
                    new_high = max(prev['high'], current['high'])
                    new_low = max(prev['low'], current['low'])  # 上升趋势取较高低点
                else:
                    new_high = min(prev['high'], current['high'])  # 下降趋势取较低高点
                    new_low = min(prev['low'], current['low'])
                # 合并K线：保留前一根的时间，更新high/low，open/close处理
                prev['high'] = new_high
                prev['low'] = new_low
                # open和close可以选择保留前一根的，或者根据趋势调整，简化：保留前一根
                # 但为了后续分型识别，我们保留open/close不变
                # 记录合并次数，防止无限循环
                i += 1
            elif current['high'] >= prev['high'] and current['low'] <= prev['low']:
                # 当前包含前一根：需要将前一根合并到当前
                # 先移除前一跟
                merged.pop()
                # 重新判断当前与新的前一根的关系（循环处理）
                continue
            else:
                # 无包含，直接加入
                merged.append(current.to_dict())
                i += 1
        
        # 转换为DataFrame
        merged_df = pd.DataFrame(merged)
        merged_df.set_index('date', inplace=True)
        return merged_df
    
    def _is_fractal(self, data: pd.DataFrame, i: int, window: int = 2) -> Tuple[bool, str]:
        """
        判断i位置是否为分型（基于合并后的K线）
        使用前后各window根K线比较，标准分型要求相邻K线不包含（已合并，故满足）
        """
        if i < window or i >= len(data) - window:
            return False, ''
        left_high = data['high'].iloc[i-window:i].max()
        left_low = data['low'].iloc[i-window:i].min()
        right_high = data['high'].iloc[i+1:i+window+1].max()
        right_low = data['low'].iloc[i+1:i+window+1].min()
        cur_high = data['high'].iloc[i]
        cur_low = data['low'].iloc[i]
        
        # 顶分型：中间高点最高，且中间低点不是最低（可选条件）
        if cur_high > left_high and cur_high > right_high:
            # 可附加条件：中间低点大于左右低点之一？标准定义只要求高点最高
            return True, 'top'
        # 底分型：中间低点最低
        if cur_low < left_low and cur_low < right_low:
            return True, 'bottom'
        return False, ''
    
    def find_fractals(self, df_merged: pd.DataFrame) -> List[Tuple[int, str, float, float]]:
        """
        识别所有分型，返回列表 (索引位置, 类型, 高点, 低点)
        """
        fractals = []
        n = len(df_merged)
        for i in range(1, n-1):
            is_f, typ = self._is_fractal(df_merged, i, window=1)  # 使用window=1简化，即相邻两根比较
            if is_f:
                fractals.append((i, typ, df_merged['high'].iloc[i], df_merged['low'].iloc[i]))
        return fractals
    
    def find_bi(self, df_merged: pd.DataFrame, fractals: List[Tuple[int, str, float, float]]) -> List[Tuple[int, int, int, float, float]]:
        """
        根据分型生成笔
        返回列表 (start_idx, end_idx, direction, start_price, end_price)
        direction: 1 向上笔, -1 向下笔
        """
        bi = []
        if len(fractals) < 2:
            return bi
        
        # 按索引排序
        fractals_sorted = sorted(fractals, key=lambda x: x[0])
        
        i = 0
        while i < len(fractals_sorted)-1:
            cur = fractals_sorted[i]
            for j in range(i+1, len(fractals_sorted)):
                nxt = fractals_sorted[j]
                # 必须类型相反
                if nxt[1] == cur[1]:
                    continue
                # 间隔至少1根K线（即索引差>=2）
                if nxt[0] - cur[0] < 2:
                    continue
                # 价格幅度要求：对于向上笔，nxt高点 > cur低点（合理）；简单使用收盘价差比例
                if cur[1] == 'bottom' and nxt[1] == 'top':
                    # 向上笔
                    start_price = cur[3]   # 低点价格（取底分型低点）
                    end_price = nxt[2]     # 高点价格（取顶分型高点）
                    if end_price > start_price * (1 + self.bi_threshold):
                        bi.append((cur[0], nxt[0], 1, start_price, end_price))
                        i = j
                        break
                elif cur[1] == 'top' and nxt[1] == 'bottom':
                    # 向下笔
                    start_price = cur[2]   # 高点价格
                    end_price = nxt[3]     # 低点价格
                    if start_price > end_price * (1 + self.bi_threshold):
                        bi.append((cur[0], nxt[0], -1, start_price, end_price))
                        i = j
                        break
            else:
                # 没找到合适的下一个分型，结束
                break
        return bi
    
    def find_zhongshu(self, bi: List[Tuple], df_merged: pd.DataFrame) -> List[Tuple[int, int, float, float]]:
        """
        识别中枢：连续三笔重叠区域
        返回列表 (start_idx, end_idx, zg, zd)
        """
        zhongshu = []
        if len(bi) < 3:
            return zhongshu
        
        # 为了方便，将笔转换为包含区间高低点的形式
        bi_with_range = []
        for b in bi:
            start, end, dir_, sp, ep = b
            # 该笔的最低点和最高点（考虑整笔K线）
            if dir_ == 1:
                high = max(df_merged['high'].iloc[start:end+1].max(), ep)
                low = min(df_merged['low'].iloc[start:end+1].min(), sp)
            else:
                high = max(df_merged['high'].iloc[start:end+1].max(), sp)
                low = min(df_merged['low'].iloc[start:end+1].min(), ep)
            bi_with_range.append((start, end, high, low))
        
        for i in range(len(bi_with_range)-2):
            b1, b2, b3 = bi_with_range[i:i+3]
            # 重叠条件：三笔的最高点的最小值 > 三笔的最低点的最大值
            high_min = min(b1[2], b2[2], b3[2])
            low_max = max(b1[3], b2[3], b3[3])
            if high_min > low_max:
                # 存在重叠，构成中枢
                zg = high_min
                zd = low_max
                start_idx = b1[0]
                end_idx = b3[1]
                zhongshu.append((start_idx, end_idx, zg, zd))
        return zhongshu
    
    def _compute_macd(self, close: pd.Series):
        """计算MACD指标，返回DIF, DEA, MACD柱"""
        # 使用pandas计算，不依赖talib
        ema_fast = close.ewm(span=self.macd_fast, adjust=False).mean()
        ema_slow = close.ewm(span=self.macd_slow, adjust=False).mean()
        dif = ema_fast - ema_slow
        dea = dif.ewm(span=self.macd_signal, adjust=False).mean()
        macd_bar = (dif - dea) * 2
        return dif, dea, macd_bar
    
    def find_mmd(self, df_merged: pd.DataFrame, bi: List[Tuple], zhongshu: List[Tuple]) -> pd.DataFrame:
        """
        识别买卖点，返回信号DataFrame，包含时间、类型、价格
        """
        if not zhongshu:
            return pd.DataFrame()
        
        # 获取最后一个中枢
        last_zs = zhongshu[-1]
        zs_start, zs_end, zg, zd = last_zs
        
        # 获取中枢之后的笔
        after_bi = [b for b in bi if b[0] > zs_end]
        if not after_bi:
            return pd.DataFrame()
        
        # 准备MACD（如果需要）
        if self.use_macd:
            dif, dea, macd_bar = self._compute_macd(df_merged['close'])
        else:
            macd_bar = None
        
        signals = []
        
        # 遍历之后的笔
        for i, b in enumerate(after_bi):
            start, end, dir_, sp, ep = b
            
            # 第一类买点：中枢之后第一笔为向下笔，且出现底分型，并背驰
            if dir_ == -1 and i == 0:  # 假设第一笔向下
                # 检查该笔终点是否为底分型（需要找到该笔结束位置附近的分型）
                # 简化：如果该笔结束时价格是局部低点，且与前一中枢比较
                if self.use_macd and macd_bar is not None:
                    # 获取该笔区间内的MACD柱面积
                    macd_slice = macd_bar.iloc[start:end+1]
                    if len(macd_slice) > 0:
                        area = macd_slice.sum()
                        # 对比中枢之前最近一段向下笔的MACD面积（如果有）
                        prev_down = [bb for bb in bi if bb[2] == -1 and bb[1] < zs_start]
                        if prev_down:
                            prev = prev_down[-1]
                            prev_slice = macd_bar.iloc[prev[0]:prev[1]+1]
                            prev_area = prev_slice.sum() if len(prev_slice)>0 else 0
                            if area > prev_area:  # 面积减小为背驰（负数面积意味着负值更大？需注意方向）
                                # 简单处理：价格新低但MACD柱线面积减小（负值变小）
                                if df_merged['low'].iloc[end] < df_merged['low'].iloc[prev[1]] and area < prev_area:
                                    signals.append((df_merged.index[end], 'buy1', ep))
                else:
                    # 无MACD，简单用价格新低且RSI背离等，这里仅做示例
                    if df_merged['low'].iloc[end] == df_merged['low'].iloc[start:end+1].min():
                        signals.append((df_merged.index[end], 'buy1', ep))
            
            # 第二类买点：一买之后向上笔的回调不破一买低点
            if signals and signals[-1][1] == 'buy1':
                buy1_idx = signals[-1][0]
                buy1_price = signals[-1][2]
                # 找到buy1之后的所有笔
                after_buy1 = [bb for bb in bi if bb[0] > df_merged.index.get_loc(buy1_idx)]
                if len(after_buy1) >= 2:
                    up = after_buy1[0]
                    down = after_buy1[1]
                    if up[2] == 1 and down[2] == -1:
                        if down[4] > buy1_price * 0.99:  # 略高于一买低点
                            signals.append((df_merged.index[down[1]], 'buy2', down[4]))
            
            # 第三类买点：向上突破中枢后，回踩笔不进入中枢
            if dir_ == 1 and ep > zg:  # 向上笔突破上轨
                # 寻找之后的向下回踩笔
                if i+1 < len(after_bi):
                    next_bi = after_bi[i+1]
                    if next_bi[2] == -1:
                        # 回踩不进入中枢：回踩低点 > 中枢上轨
                        if next_bi[4] > zg:
                            signals.append((df_merged.index[next_bi[1]], 'buy3', next_bi[4]))
        
        # 转换为DataFrame
        if signals:
            df_signals = pd.DataFrame(signals, columns=['date', 'signal', 'price'])
            df_signals.set_index('date', inplace=True)
            return df_signals
        else:
            return pd.DataFrame()
    
    def run(self, df: pd.DataFrame) -> pd.DataFrame:
        """
        主运行函数，输入原始日线数据，输出信号DataFrame
        df 必须包含 'open','high','low','close'，索引为datetime
        """
        # 1. 合并K线
        df_merged = self.merge_k_lines(df)
        self.k_merged = df_merged
        
        # 2. 分型
        self.fractals = self.find_fractals(df_merged)
        
        # 3. 笔
        self.bi = self.find_bi(df_merged, self.fractals)
        
        # 4. 中枢
        self.zhongshu = self.find_zhongshu(self.bi, df_merged)
        
        # 5. 买卖点
        self.signals = self.find_mmd(df_merged, self.bi, self.zhongshu)
        
        return self.signals
    
    def get_summary(self) -> dict:
        """获取分析摘要"""
        return {
            'merged_k_count': len(self.k_merged) if self.k_merged is not None else 0,
            'fractal_count': len(self.fractals),
            'bi_count': len(self.bi),
            'zhongshu_count': len(self.zhongshu),
            'signal_count': len(self.signals),
        }
    
    def get_bi_list(self) -> List[dict]:
        """获取笔列表"""
        result = []
        for b in self.bi:
            start, end, dir_, sp, ep = b
            result.append({
                'start_idx': start,
                'end_idx': end,
                'direction': 'up' if dir_ == 1 else 'down',
                'start_price': sp,
                'end_price': ep,
            })
        return result
    
    def get_zhongshu_list(self) -> List[dict]:
        """获取中枢列表"""
        result = []
        for zs in self.zhongshu:
            start, end, zg, zd = zs
            result.append({
                'start_idx': start,
                'end_idx': end,
                'zg': zg,  # 中枢高点
                'zd': zd,  # 中枢低点
            })
        return result


# ================= 便捷函数 =================

def analyze_stock(df: pd.DataFrame, **kwargs) -> dict:
    """
    便捷函数：分析单只股票
    
    Args:
        df: 必须包含 open, high, low, close 列，索引为日期
        **kwargs: ChanQuantEngine的参数
    
    Returns:
        dict: 包含 signals, bi, zhongshu, summary 等
    """
    engine = ChanQuantEngine(**kwargs)
    signals = engine.run(df)
    
    return {
        'signals': signals,
        'bi': engine.get_bi_list(),
        'zhongshu': engine.get_zhongshu_list(),
        'summary': engine.get_summary(),
        'k_merged': engine.k_merged,
    }


# ================= 测试代码 =================
if __name__ == '__main__':
    # 模拟数据测试
    import random
    from datetime import datetime, timedelta
    
    # 生成模拟K线数据
    n = 200
    dates = [datetime(2024, 1, 1) + timedelta(days=i) for i in range(n)]
    
    # 随机生成涨跌
    base_price = 100
    prices = []
    for i in range(n):
        change = random.uniform(-3, 3)
        base_price *= (1 + change/100)
        prices.append(base_price)
    
    data = pd.DataFrame({
        'open': [p * random.uniform(0.98, 1.02) for p in prices],
        'high': [p * random.uniform(1.00, 1.05) for p in prices],
        'low': [p * random.uniform(0.95, 1.00) for p in prices],
        'close': prices,
    }, index=dates)
    
    # 运行缠论分析
    engine = ChanQuantEngine(bi_threshold=0.03, use_macd=False)
    signals = engine.run(data)
    
    print("=" * 50)
    print("缠论分析结果")
    print("=" * 50)
    
    summary = engine.get_summary()
    print(f"合并后K线数: {summary['merged_k_count']}")
    print(f"分型数量: {summary['fractal_count']}")
    print(f"笔数量: {summary['bi_count']}")
    print(f"中枢数量: {summary['zhongshu_count']}")
    print(f"信号数量: {summary['signal_count']}")
    
    if len(signals) > 0:
        print("\n买卖点信号:")
        print(signals)
    
    print("\n笔列表:")
    for bi in engine.get_bi_list()[:5]:
        print(f"  {bi['direction']}: {bi['start_price']:.2f} -> {bi['end_price']:.2f}")
