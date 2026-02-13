#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
综合选股系统
功能：融合技术面、基本面、缠论的全面选股系统
"""

import pandas as pd
import numpy as np
from typing import List, Dict, Optional
from datetime import datetime

# 导入各模块
from data_source import EastMoneyData, get_quotes, get_kline, get_index
from fundamental import FundamentalSelector
from chanlun_engine import ChanQuantEngine
from sector_analysis import SectorAnalysis, SectorSelector


class ComprehensiveSelector:
    """
    综合选股器
    
    融合:
    - 技术面: 均线多头、MACD金叉、成交量放大
    - 基本面: PE/ROE/营收增长筛选
    - 缠论: 笔/中枢结构、买卖点信号
    - 板块效应: 强势板块领涨股
    """
    
    def __init__(self):
        self.em = EastMoneyData()
        self.fs = FundamentalSelector()
        self.chan = ChanQuantEngine(bi_threshold=0.03, use_macd=True)
        self.sector = SectorSelector()  # 板块效应选股器
    
    def get_technical_stocks(self, count=200) -> pd.DataFrame:
        """
        技术面筛选
        
        条件:
        - 涨幅 > 3%
        - 成交额 > 1亿
        - 换手率 > 3%
        """
        df = self.em.get_realtime_quotes(count * 2)
        
        # 筛选条件
        filtered = df[
            (df['涨跌幅'] > 3) &                    # 涨幅 > 3%
            (df['成交额'] > 1e8) &                   # 成交额 > 1亿
            (df['换手率'] > 3)                       # 换手率 > 3%
        ].copy()
        
        # 按涨跌幅排序
        filtered = filtered.sort_values('涨跌幅', ascending=False)
        
        return filtered.head(count)
    
    def get_fundamental_stocks(self, count=100) -> pd.DataFrame:
        """基本面筛选"""
        df = self.fs.get_stock_list_with_fundamental(count * 2)
        
        # 基础筛选
        filtered = df[
            (df['成交额'] > 5e7) &                   # 成交额 > 5000万
            (~df['名称'].str.contains('ST|退', na=False))  # 排除ST
        ].copy()
        
        # 计算评分
        scored = self.fs.calculate_score(filtered)
        
        return scored.head(count)
    
    def analyze_stock_technical(self, symbol: str) -> Dict:
        """
        分析单只股票的技术面
        
        Returns:
            dict: 技术分析结果
        """
        result = {
            'symbol': symbol,
            '趋势': '未知',
            'MACD': '未知',
            'KDJ': '未知',
            '信号': []
        }
        
        try:
            # 获取K线
            kline = self.em.get_stock_kline(symbol, start_date='20240101')
            if kline is None or len(kline) < 60:
                return result
            
            # 计算技术指标
            close = kline['收盘']
            
            # 均线判断
            ma20 = close.rolling(20).mean()
            ma60 = close.rolling(60).mean()
            
            if ma20.iloc[-1] > ma60.iloc[-1]:
                result['趋势'] = '多头↑'
            elif ma20.iloc[-1] < ma60.iloc[-1]:
                result['趋势'] = '空头↓'
            else:
                result['趋势'] = '震荡→'
            
            # MACD
            ema12 = close.ewm(span=12).mean()
            ema26 = close.ewm(span=26).mean()
            dif = ema12 - ema26
            dea = dif.ewm(span=9).mean()
            macd = (dif - dea) * 2
            
            if dif.iloc[-1] > dea.iloc[-1]:
                result['MACD'] = '金叉↑'
                result['信号'].append('MACD金叉')
            else:
                result['MACD'] = '死叉↓'
            
            # KDJ
            low_9 = kline['最低'].rolling(9).min()
            high_9 = kline['最高'].rolling(9).max()
            rsv = (close - low_9) / (high_9 - low_9) * 100
            k = rsv.ewm(3).mean()
            d = k.ewm(3).mean()
            j = 3 * k - 2 * d
            
            if j.iloc[-1] > 100:
                result['KDJ'] = '超买'
            elif j.iloc[-1] < 0:
                result['KDJ'] = '超卖'
                result['信号'].append('KDJ超卖')
            else:
                result['KDJ'] = '正常'
            
            # 成交量判断
            vol_ma5 = kline['成交量'].rolling(5).mean()
            if kline['成交量'].iloc[-1] > vol_ma5.iloc[-1] * 1.5:
                result['信号'].append('放量')
            
            # 趋势判断
            if result['趋势'] == '多头↑' and result['MACD'] == '金叉↑':
                result['信号'].append('强势突破')
            
        except Exception as e:
            result['error'] = str(e)
        
        return result
    
    def analyze_stock_chanlun(self, symbol: str) -> Dict:
        """
        分析单只股票的缠论结构
        """
        result = {
            'symbol': symbol,
            '笔数': 0,
            '中枢数': 0,
            '结构': '未知',
            '信号': []
        }
        
        try:
            kline = self.em.get_stock_kline(symbol, start_date='20240101')
            if kline is None or len(kline) < 100:
                return result
            
            # 重命名列
            df = kline.rename(columns={
                '开盘': 'open', '收盘': 'close', 
                '最高': 'high', '最低': 'low'
            })
            
            # 运行缠论分析
            self.chan.run(df)
            
            # 获取结果
            summary = self.chan.get_summary()
            result['笔数'] = summary['bi_count']
            result['中枢数'] = summary['zhongshu_count']
            
            # 结构判断
            if summary['zhongshu_count'] > 0:
                # 有中枢，看当前笔的方向
                bi_list = self.chan.get_bi_list()
                if bi_list:
                    last_bi = bi_list[-1]
                    if last_bi['direction'] == 'up':
                        result['结构'] = '上涨中枢'
                        result['信号'].append('笔向上')
                    else:
                        result['结构'] = '下跌中枢'
                        result['信号'].append('笔向下')
            else:
                result['结构'] = '无中枢'
            
            # 买卖点信号
            signals = self.chan.signals
            if len(signals) > 0:
                for idx, row in signals.iterrows():
                    result['信号'].append(f"{row['signal']}:{row['price']:.2f}")
            
        except Exception as e:
            result['error'] = str(e)
        
        return result
    
    def comprehensive_analysis(self, symbols: List[str] = None, top_n: int = 20) -> pd.DataFrame:
        """
        综合分析选股
        
        步骤:
        1. 获取涨幅前N的股票
        2. 技术面筛选
        3. 基本面评分
        4. 缠论结构分析(可选)
        """
        print("=" * 60)
        print("综合选股分析")
        print("=" * 60)
        
        # 1. 获取候选股票
        print("\n📊 步骤1: 获取候选股票...")
        candidates = self.em.get_realtime_quotes(top_n * 3)
        print(f"   获取到 {len(candidates)} 只候选股票")
        
        # 2. 技术面筛选
        print("\n🔍 步骤2: 技术面筛选...")
        tech_filtered = candidates[
            (candidates['涨跌幅'] > 2) &              # 涨幅 > 2%
            (candidates['成交额'] > 1e8)              # 成交额 > 1亿
        ].copy()
        print(f"   技术面筛选后: {len(tech_filtered)} 只")
        
        # 3. 基本面评分
        print("\n📈 步骤3: 基本面评分...")
        # 合并基本面数据
        fund_data = self.fs.get_stock_list_with_fundamental(500)
        
        # 合并
        merged = tech_filtered.merge(
            fund_data[['代码', '市盈率', '市净率', '净资产收益率', '净利润同比增长', '营收同比增长']],
            on='代码',
            how='left',
            suffixes=('', '_fund')
        )
        
        # 计算综合得分
        scored = self.fs.calculate_score(merged)
        
        # 4. 选取Top N
        result = scored.head(top_n)
        
        print(f"\n✅ 综合选股结果: {len(result)} 只")
        
        return result
    
    def get_buy_signals(self, symbols: List[str] = None) -> pd.DataFrame:
        """
        获取买入信号股票
        
        筛选条件:
        - 技术面: 多头排列 + MACD金叉
        - 基本面: 综合得分 > 40
        """
        print("=" * 60)
        print("买入信号筛选")
        print("=" * 60)
        
        # 获取候选
        if symbols is None:
            candidates = self.em.get_realtime_quotes(100)
        else:
            # 需要单独获取
            candidates = self.em.get_realtime_quotes(len(symbols))
        
        # 筛选
        signals = []
        
        for _, row in candidates.iterrows():
            symbol = row['代码']
            
            # 技术分析
            tech = self.analyze_stock_technical(symbol)
            
            # 基本面分析
            fund = self.fs.get_stock_list_with_fundamental(200)
            fund_stock = fund[fund['代码'] == symbol]
            
            if len(fund_stock) > 0:
                score = fund_stock.iloc[0]
                fund_score = self.fs.calculate_score(fund_stock).iloc[0]['综合得分']
            else:
                fund_score = 0
            
            # 判断买入信号
            is_buy = False
            reasons = []
            
            if tech['趋势'] == '多头↑':
                is_buy = True
                reasons.append('多头排列')
            
            if tech['MACD'] == '金叉↑':
                is_buy = True
                reasons.append('MACD金叉')
            
            if '放量' in tech['信号']:
                is_buy = True
                reasons.append('放量突破')
            
            if fund_score > 40:
                is_buy = True
                reasons.append(f'基本面优({fund_score:.0f})')
            
            if is_buy:
                signals.append({
                    '代码': symbol,
                    '名称': row['名称'],
                    '最新价': row['最新价'],
                    '涨跌幅': row['涨跌幅'],
                    '趋势': tech['趋势'],
                    'MACD': tech['MACD'],
                    'KDJ': tech['KDJ'],
                    '基本面得分': fund_score,
                    '信号': ','.join(reasons) if reasons else '综合信号'
                })
        
        return pd.DataFrame(signals)
    
    def get_sector_effect_stocks(self, min_sector_rps: float = 70, 
                                   min_stock_change: float = 3.0) -> pd.DataFrame:
        """
        获取板块效应领涨股
        
        基于强势板块中的领涨股进行选股
        
        Args:
            min_sector_rps: 最小板块RPS强度
            min_stock_change: 最小个股涨幅
        
        Returns:
            板块效应领涨股列表
        """
        print("=" * 60)
        print("板块效应选股")
        print("=" * 60)
        
        # 获取板块效应领涨股
        df = self.sector.select_by_sector_effect(
            min_sector_rps=min_sector_rps,
            min_stock_change=min_stock_change
        )
        
        if len(df) == 0:
            print("暂无符合条件的板块效应股")
            return pd.DataFrame()
        
        print(f"\n✅ 找到 {len(df)} 只板块效应领涨股")
        
        return df
    
    def get_full_analysis(self) -> Dict:
        """
        获取完整分析报告
        
        包含：
        - 市场状态
        - 强势板块
        - 板块效应股
        - 技术面信号股
        - 基本面优质股
        """
        print("=" * 60)
        print("完整选股分析报告")
        print("=" * 60)
        
        # 1. 市场状态
        print("\n📊 市场状态分析...")
        market = self.sector.sector_analysis.get_market_context()
        
        # 2. 强势板块
        print("\n🔥 强势板块TOP10...")
        strong_sectors = self.sector.sector_analysis.get_sector_strength(10)
        
        # 3. 板块效应选股
        print("\n🚀 板块效应选股...")
        sector_stocks = self.sector.select_by_sector_effect(
            min_sector_rps=70, 
            min_stock_change=3.0
        )
        
        # 4. 技术面选股
        print("\n📈 技术面选股...")
        tech_stocks = self.get_buy_signals()
        
        # 5. 基本面选股
        print("\n💰 基本面选股...")
        fund_stocks = self.fs.get_value_stocks()[:10]
        
        return {
            '市场状态': market,
            '强势板块': strong_sectors,
            '板块效应股': sector_stocks,
            '技术信号股': tech_stocks,
            '基本面股': fund_stocks,
        }


# ================= 便捷函数 =================

def get_buy_signals() -> pd.DataFrame:
    """获取买入信号"""
    selector = ComprehensiveSelector()
    return selector.get_buy_signals()


def comprehensive_select(top_n: int = 20) -> pd.DataFrame:
    """综合选股"""
    selector = ComprehensiveSelector()
    return selector.comprehensive_analysis(top_n=top_n)


def sector_effect_select(min_rps: float = 70, min_change: float = 3.0) -> pd.DataFrame:
    """板块效应选股"""
    selector = ComprehensiveSelector()
    return selector.get_sector_effect_stocks(min_sector_rps=min_rps, min_stock_change=min_change)


def full_analysis() -> Dict:
    """完整分析报告"""
    selector = ComprehensiveSelector()
    return selector.get_full_analysis()


def analyze_stock(symbol: str) -> Dict:
    """分析单只股票"""
    selector = ComprehensiveSelector()
    tech = selector.analyze_stock_technical(symbol)
    chan = selector.analyze_stock_chanlun(symbol)
    return {
        '技术面': tech,
        '缠论': chan
    }


# ================= 测试代码 =================

if __name__ == '__main__':
    selector = ComprehensiveSelector()
    
    print("=" * 60)
    print("综合选股系统测试")
    print("=" * 60)
    
    # 1. 综合选股
    print("\n📊 综合选股...")
    result = selector.comprehensive_analysis(top_n=10)
    
    print("\n选股结果:")
    for i, row in result.iterrows():
        print(f"  {row['代码']} {row['名称']:<8} "
              f"价:{row['最新价']:>6.2f} "
              f"涨:{row['涨跌幅']:>5.1f}% "
              f"得分:{row['综合得分']:>5.1f}")
    
    # 2. 买入信号
    print("\n🎯 买入信号股票...")
    buy_signals = selector.get_buy_signals()
    print(f"找到 {len(buy_signals)} 只买入信号股")
    
    if len(buy_signals) > 0:
        for _, row in buy_signals.head(10).iterrows():
            print(f"  {row['代码']} {row['名称']:<8} "
                  f"{row['趋势']} {row['MACD']} "
                  f"基本面:{row['基本面得分']:.0f}")
            print(f"     信号: {row['信号']}")
    
    # 3. 单股分析
    print("\n📈 个股分析 (000001 平安银行)...")
    analysis = selector.analyze_stock_technical('000001')
    print(f"   趋势: {analysis['趋势']}")
    print(f"   MACD: {analysis['MACD']}")
    print(f"   KDJ: {analysis['KDJ']}")
    print(f"   信号: {analysis['信号']}")
