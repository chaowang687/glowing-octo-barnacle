#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
缠论分析模块 - 用于界面展示
集成缠论引擎，提供买卖点信号
"""

import pandas as pd
import numpy as np
from typing import Dict, List, Tuple
from chanlun_engine import ChanQuantEngine


class ChanlunAnalyzer:
    """缠论分析器 - 用于界面展示"""
    
    def __init__(self):
        pass
    
    def analyze(self, kline: pd.DataFrame) -> Dict:
        """
        分析K线，返回缠论分析结果
        
        Args:
            kline: K线数据，包含 high, low, open, close, volume
            
        Returns:
            Dict: 分析结果
        """
        if kline is None or len(kline) < 30:
            return {
                'status': '数据不足',
                'bi': [],
                'segments': [],
                'zhongshu': [],
                'signals': [],
                'trend': '未知',
                'summary': '数据不足，需要至少30根K线'
            }
        
        try:
            # 使用缠论引擎
            engine = ChanQuantEngine(
                bi_threshold=0.03,  # 3%以上才形成有效笔
                use_macd=False
            )
            
            # 合并K线
            merged = engine.merge_k_lines(kline)
            
            # 识别分型
            fractals = engine.find_fractals(merged)
            
            # 识别笔
            bi_list = engine.find_bi(merged, fractals)
            
            # 识别中枢
            zhongshu = engine.find_zhongshu(bi_list, merged)
            
            # 生成买卖点
            signals = engine.find_mmd(merged, bi_list, zhongshu)
            
            # 判断趋势
            if len(bi_list) >= 2:
                last_bi = bi_list[-1]
                prev_bi = bi_list[-2]
                if last_bi[2] == 1:
                    trend = '上涨笔'
                else:
                    trend = '下跌笔'
            elif len(bi_list) == 1:
                trend = '起始笔'
            else:
                trend = '整理'
            
            # 生成简报
            summary = self._generate_summary(bi_list, zhongshu, trend)
            
            # 转换信号格式
            signal_list = []
            if len(signals) > 0:
                for _, row in signals.iterrows():
                    signal_list.append({
                        'type': row.get('type', ''),
                        'date': str(row.get('date', '')),
                        'price': row.get('price', 0),
                        'direction': row.get('direction', '')
                    })
            
            return {
                'status': '成功',
                'bi': bi_list,
                'segments': [],
                'zhongshu': zhongshu,
                'signals': signal_list,
                'trend': trend,
                'summary': summary,
                'fractals': fractals
            }
            
        except Exception as e:
            return {
                'status': '分析失败',
                'error': str(e),
                'bi': [],
                'segments': [],
                'zhongshu': [],
                'signals': [],
                'trend': '未知',
                'summary': f'分析出错: {str(e)}'
            }
    
    def _generate_summary(self, bi_list: List, zhongshu: List, trend: str) -> str:
        """生成分析简报"""
        parts = []
        
        # 笔的数量
        up_bi = sum(1 for b in bi_list if b[2] == 1)
        down_bi = sum(1 for b in bi_list if b[2] == -1)
        parts.append(f"笔: ↑{up_bi}笔/↓{down_bi}笔")
        
        # 中枢
        parts.append(f"中枢: {len(zhongshu)}个")
        
        # 趋势
        parts.append(f"当前: {trend}")
        
        return " | ".join(parts)
    
    def get_buy_signals(self, kline: pd.DataFrame) -> List[Dict]:
        """获取买入信号"""
        result = self.analyze(kline)
        buy_signals = []
        
        for sig in result.get('signals', []):
            if '买' in sig.get('type', ''):
                buy_signals.append(sig)
        
        return buy_signals
    
    def get_sell_signals(self, kline: pd.DataFrame) -> List[Dict]:
        """获取卖出信号"""
        result = self.analyze(kline)
        sell_signals = []
        
        for sig in result.get('signals', []):
            if '卖' in sig.get('type', ''):
                sell_signals.append(sig)
        
        return sell_signals


def analyze_stock(symbol: str) -> Dict:
    """
    便捷函数：分析单只股票
    
    Args:
        symbol: 股票代码
        
    Returns:
        Dict: 分析结果
    """
    from tencent_source import TencentDataSource
    
    # 获取K线
    source = TencentDataSource()
    kline = source.get_stock_kline(symbol, period='day')
    
    if len(kline) < 30:
        return {
            'status': '数据不足',
            'summary': f'数据不足，当前只有{len(kline)}根K线'
        }
    
    # 分析
    analyzer = ChanlunAnalyzer()
    return analyzer.analyze(kline)


# 测试
if __name__ == '__main__':
    print("=" * 50)
    print("缠论分析模块测试")
    print("=" * 50)
    
    # 测试分析
    from tencent_source import TencentDataSource
    
    source = TencentDataSource()
    kline = source.get_stock_kline('600519')
    
    if len(kline) > 30:
        analyzer = ChanlunAnalyzer()
        result = analyzer.analyze(kline)
        
        print(f"\n状态: {result['status']}")
        print(f"趋势: {result['trend']}")
        print(f"简报: {result['summary']}")
        print(f"买入信号: {len(analyzer.get_buy_signals(kline))}个")
        print(f"卖出信号: {len(analyzer.get_sell_signals(kline))}个")
        
        if result.get('signals'):
            print("\n信号详情:")
            for sig in result['signals'][:5]:
                print(f"  {sig}")
    else:
        print(f"数据不足: {len(kline)}根")
    
    print("\n✅ 测试完成")
