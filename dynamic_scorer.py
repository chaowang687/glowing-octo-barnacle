#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
动态评分函数
根据解析出的评分公式计算股票评分
"""

import pandas as pd
import numpy as np
from typing import Dict, List

class DynamicScorer:
    """
    动态评分器
    """
    
    def __init__(self, formula_info: Dict):
        """
        初始化评分器
        
        Args:
            formula_info: 解析后的评分公式信息
        """
        self.formula_info = formula_info
    
    def calculate_score(self, kline_data: pd.DataFrame) -> int:
        """
        根据评分公式计算评分
        
        Args:
            kline_data: K线数据
            
        Returns:
            int: 评分
        """
        score = 0
        
        # 计算趋势强度得分
        score += self._calculate_trend_strength(kline_data)
        
        # 计算动量确认得分
        score += self._calculate_momentum_confirmation(kline_data)
        
        # 计算量价配合得分
        score += self._calculate_volume_price_coordination(kline_data)
        
        # 计算风险控制得分
        score += self._calculate_risk_control(kline_data)
        
        # 计算市场环境得分
        score += self._calculate_market_environment(kline_data)
        
        # 应用扣分项
        score -= self._calculate_penalties(kline_data)
        
        # 限制评分范围
        score = max(0, min(100, score))
        
        return score

    def calculate_score_detail(self, kline_data: pd.DataFrame):
        detail = {
            'section_scores': {},
            'triggered_items': [],
            'recognized_not_triggered_items': [],
            'unrecognized_items': [],
            'penalty_items': [],
        }

        score = 0

        trend_score = self._calculate_trend_strength(kline_data, detail)
        detail['section_scores']['trend_strength'] = trend_score
        score += trend_score

        momentum_score = self._calculate_momentum_confirmation(kline_data, detail)
        detail['section_scores']['momentum_confirmation'] = momentum_score
        score += momentum_score

        volume_score = self._calculate_volume_price_coordination(kline_data, detail)
        detail['section_scores']['volume_price_coordination'] = volume_score
        score += volume_score

        risk_score = self._calculate_risk_control(kline_data, detail)
        detail['section_scores']['risk_control'] = risk_score
        score += risk_score

        market_score = self._calculate_market_environment(kline_data, detail)
        detail['section_scores']['market_environment'] = market_score
        score += market_score

        penalty = self._calculate_penalties(kline_data, detail)
        detail['section_scores']['penalty_total'] = penalty
        score -= penalty

        score = max(0, min(100, score))
        return score, detail

    def _calculate_kdj(self, kline_data: pd.DataFrame, n: int = 9) -> Dict[str, float]:
        if len(kline_data) < n:
            return {'k': np.nan, 'd': np.nan, 'j': np.nan}
        low_n = kline_data['最低'].rolling(n, min_periods=n).min()
        high_n = kline_data['最高'].rolling(n, min_periods=n).max()
        denom = (high_n - low_n).replace(0, np.nan)
        rsv = (kline_data['收盘'] - low_n) / denom * 100
        k = rsv.ewm(alpha=1/3, adjust=False).mean()
        d = k.ewm(alpha=1/3, adjust=False).mean()
        j = 3 * k - 2 * d
        return {'k': float(k.iloc[-1]), 'd': float(d.iloc[-1]), 'j': float(j.iloc[-1])}
    
    def _calculate_trend_strength(self, kline_data: pd.DataFrame, detail: Dict = None) -> int:
        """
        计算趋势强度得分
        """
        trend_score = 0
        items = self.formula_info.get('trend_strength', {}).get('items', [])
        
        for item in items:
            condition = item.get('condition', '')
            item_score = item.get('score', 0)
            recognized = False
            triggered = False
            
            if 'MA5 > MA20' in condition:
                recognized = True
                if len(kline_data) >= 20:
                    ma5 = kline_data['收盘'].tail(5).mean()
                    ma20 = kline_data['收盘'].tail(20).mean()
                    if ma5 > ma20:
                        trend_score += item_score
                        triggered = True
            
            elif 'MA10 > MA30' in condition:
                recognized = True
                if len(kline_data) >= 30:
                    ma10 = kline_data['收盘'].tail(10).mean()
                    ma30 = kline_data['收盘'].tail(30).mean()
                    if ma10 > ma30:
                        trend_score += item_score
                        triggered = True
            
            elif '均线多头排列' in condition:
                recognized = True
                if len(kline_data) >= 20:
                    ma5 = kline_data['收盘'].tail(5).mean()
                    ma10 = kline_data['收盘'].tail(10).mean()
                    ma20 = kline_data['收盘'].tail(20).mean()
                    if ma5 > ma10 > ma20:
                        trend_score += item_score
                        triggered = True
            
            elif '近期5日涨幅 > 3%' in condition:
                recognized = True
                if len(kline_data) >= 5:
                    start_price = kline_data['收盘'].iloc[-5]
                    end_price = kline_data['收盘'].iloc[-1]
                    change_pct = (end_price - start_price) / start_price * 100
                    if change_pct > 3:
                        trend_score += item_score
                        triggered = True

            if detail is not None:
                payload = {'section': 'trend_strength', 'condition': condition, 'score': item_score}
                if recognized:
                    if triggered:
                        detail['triggered_items'].append(payload)
                    else:
                        detail['recognized_not_triggered_items'].append(payload)
                else:
                    detail['unrecognized_items'].append(payload)
        
        return trend_score
    
    def _calculate_momentum_confirmation(self, kline_data: pd.DataFrame, detail: Dict = None) -> int:
        """
        计算动量确认得分
        """
        momentum_score = 0
        items = self.formula_info.get('momentum_confirmation', {}).get('items', [])
        
        for item in items:
            condition = item.get('condition', '')
            item_score = item.get('score', 0)
            recognized = False
            triggered = False
            
            if 'MACD金叉' in condition:
                recognized = True
                # 简化计算MACD
                if len(kline_data) >= 26:
                    exp1 = kline_data['收盘'].ewm(span=12, adjust=False).mean()
                    exp2 = kline_data['收盘'].ewm(span=26, adjust=False).mean()
                    macd = exp1 - exp2
                    signal = macd.ewm(span=9, adjust=False).mean()
                    histogram = macd - signal
                    if len(histogram) >= 2:
                        if histogram.iloc[-1] > histogram.iloc[-2] and histogram.iloc[-1] > 0:
                            momentum_score += item_score
                            triggered = True
            
            elif 'KDJ' in condition:
                recognized = True
                # 计算KDJ
                if len(kline_data) >= 9:
                    kdj = self._calculate_kdj(kline_data, n=9)
                    k = kdj['k']
                    d = kdj['d']
                    if not np.isnan(k) and not np.isnan(d):
                        if k > d and 20 < k < 80:
                            momentum_score += item_score
                            triggered = True
            
            elif '布林带' in condition:
                recognized = True
                if len(kline_data) >= 20:
                    close = kline_data['收盘'].iloc[-1]
                    ma20 = kline_data['收盘'].tail(20).mean()
                    std20 = kline_data['收盘'].tail(20).std()
                    upper_band = ma20 + 2 * std20
                    lower_band = ma20 - 2 * std20
                    middle_band = ma20
                    if close > middle_band:
                        momentum_score += item_score
                        triggered = True

            if detail is not None:
                payload = {'section': 'momentum_confirmation', 'condition': condition, 'score': item_score}
                if recognized:
                    if triggered:
                        detail['triggered_items'].append(payload)
                    else:
                        detail['recognized_not_triggered_items'].append(payload)
                else:
                    detail['unrecognized_items'].append(payload)
        
        return momentum_score
    
    def _calculate_volume_price_coordination(self, kline_data: pd.DataFrame, detail: Dict = None) -> int:
        """
        计算量价配合得分
        """
        volume_score = 0
        items = self.formula_info.get('volume_price_coordination', {}).get('items', [])
        
        for item in items:
            condition = item.get('condition', '')
            item_score = item.get('score', 0)
            recognized = False
            triggered = False
            
            if '成交量 > 20日均量1.3倍' in condition:
                recognized = True
                if len(kline_data) >= 20:
                    avg_volume = kline_data['成交量'].tail(20).mean()
                    recent_volume = kline_data['成交量'].iloc[-1]
                    if recent_volume > avg_volume * 1.3:
                        volume_score += item_score
                        triggered = True
            
            elif '量比' in condition:
                recognized = True
                if len(kline_data) >= 5:
                    avg_volume_5 = kline_data['成交量'].tail(5).mean()
                    current_volume = kline_data['成交量'].iloc[-1]
                    if current_volume / avg_volume_5 > 1.2:
                        volume_score += item_score
                        triggered = True

            if detail is not None:
                payload = {'section': 'volume_price_coordination', 'condition': condition, 'score': item_score}
                if recognized:
                    if triggered:
                        detail['triggered_items'].append(payload)
                    else:
                        detail['recognized_not_triggered_items'].append(payload)
                else:
                    detail['unrecognized_items'].append(payload)
        
        return volume_score
    
    def _calculate_risk_control(self, kline_data: pd.DataFrame, detail: Dict = None) -> int:
        """
        计算风险控制得分
        """
        risk_score = 0
        items = self.formula_info.get('risk_control', {}).get('items', [])
        
        for item in items:
            condition = item.get('condition', '')
            item_score = item.get('score', 0)
            recognized = False
            triggered = False
            
            if '波动率' in condition:
                recognized = True
                if len(kline_data) >= 30:
                    returns = kline_data['收盘'].pct_change().tail(10).std()
                    median_volatility = kline_data['收盘'].pct_change().tail(30).std()
                    if returns < median_volatility:
                        risk_score += item_score
                        triggered = True
            
            elif '价格处于20日均线上方' in condition:
                recognized = True
                if len(kline_data) >= 20:
                    ma20 = kline_data['收盘'].tail(20).mean()
                    current_price = kline_data['收盘'].iloc[-1]
                    if current_price > ma20:
                        deviation = (current_price - ma20) / ma20 * 100
                        if deviation < 8:
                            risk_score += item_score
                            triggered = True
            
            elif 'RSI' in condition:
                recognized = True
                if len(kline_data) >= 14:
                    returns = kline_data['收盘'].pct_change().tail(14)
                    gains = returns[returns > 0].mean()
                    losses = -returns[returns < 0].mean()
                    if losses > 0:
                        rs = gains / losses
                        rsi = 100 - (100 / (1 + rs))
                        if 40 < rsi < 60:
                            risk_score += item_score
                            triggered = True

            if detail is not None:
                payload = {'section': 'risk_control', 'condition': condition, 'score': item_score}
                if recognized:
                    if triggered:
                        detail['triggered_items'].append(payload)
                    else:
                        detail['recognized_not_triggered_items'].append(payload)
                else:
                    detail['unrecognized_items'].append(payload)
        
        return risk_score
    
    def _calculate_market_environment(self, kline_data: pd.DataFrame, detail: Dict = None) -> int:
        """
        计算市场环境得分
        """
        market_score = 0
        items = self.formula_info.get('market_environment', {}).get('items', [])
        if not items or len(kline_data) < 20:
            return 0

        close = kline_data['收盘']
        base = close.iloc[-20]
        stock_ret_20 = (close.iloc[-1] - base) / base * 100 if base else 0

        fallback_score = 0
        chosen = None
        for item in items:
            condition = item.get('condition', '')
            item_score = item.get('score', 0)
            cond_lower = str(condition).lower()

            if '其他' in condition:
                fallback_score = max(fallback_score, item_score)
                continue

            if ('上涨' in condition or '涨幅' in condition or 'up' in cond_lower) and stock_ret_20 > 3:
                market_score = max(market_score, item_score)
                chosen = {'section': 'market_environment', 'condition': condition, 'score': item_score}

        final_score = market_score if market_score > 0 else fallback_score

        if detail is not None:
            for item in items:
                payload = {'section': 'market_environment', 'condition': item.get('condition', ''), 'score': item.get('score', 0)}
                if chosen and payload['condition'] == chosen['condition'] and payload['score'] == chosen['score']:
                    detail['triggered_items'].append(payload)
                elif '其他' in payload['condition'] and final_score == payload['score']:
                    detail['triggered_items'].append(payload)
                else:
                    detail['recognized_not_triggered_items'].append(payload)

        return final_score
    
    def _calculate_penalties(self, kline_data: pd.DataFrame, detail: Dict = None) -> int:
        """
        计算扣分项
        """
        penalty = 0
        items = self.formula_info.get('penalty_items', [])
        
        for item in items:
            condition = item.get('condition', '')
            item_penalty = item.get('penalty', 0)
            recognized = False
            triggered = False
            
            if '长上影线' in condition:
                recognized = True
                if len(kline_data) >= 1:
                    high = kline_data['最高'].iloc[-1]
                    close = kline_data['收盘'].iloc[-1]
                    open_price = kline_data['开盘'].iloc[-1]
                    amplitude = (high - min(open_price, close)) / min(open_price, close) * 100
                    if amplitude > 5 and (high - close) / high * 100 > 2:
                        penalty += item_penalty
                        triggered = True
            
            elif '行业指数' in condition:
                recognized = True
                # 简化计算，默认不扣分
                pass
            
            elif '大宗交易' in condition:
                recognized = True
                # 简化计算，默认不扣分
                pass
            
            elif '涨幅>5%但波动率同步放大' in condition:
                recognized = True
                if len(kline_data) >= 5:
                    start_price = kline_data['收盘'].iloc[-5]
                    end_price = kline_data['收盘'].iloc[-1]
                    change_pct = (end_price - start_price) / start_price * 100
                    if change_pct > 5:
                        volatility = kline_data['收盘'].pct_change().tail(10).std()
                        if volatility > kline_data['收盘'].pct_change().tail(30).std():
                            penalty += item_penalty
                            triggered = True
            
            elif '价涨量缩' in condition:
                recognized = True
                if len(kline_data) >= 2:
                    price_change = kline_data['收盘'].iloc[-1] - kline_data['收盘'].iloc[-2]
                    volume_change = kline_data['成交量'].iloc[-1] - kline_data['成交量'].iloc[-2]
                    if price_change > 0 and volume_change < 0:
                        penalty += item_penalty
                        triggered = True
            
            elif 'RSI' in condition:
                recognized = True
                if len(kline_data) >= 14:
                    returns = kline_data['收盘'].pct_change().tail(14)
                    gains = returns[returns > 0].mean()
                    losses = -returns[returns < 0].mean()
                    if losses > 0:
                        rs = gains / losses
                        rsi = 100 - (100 / (1 + rs))
                        if rsi > 70 or rsi < 30:
                            penalty += item_penalty
                            triggered = True

            if detail is not None:
                payload = {'section': 'penalty_items', 'condition': condition, 'penalty': item_penalty}
                if recognized:
                    if triggered:
                        detail['penalty_items'].append(payload)
                    else:
                        detail['recognized_not_triggered_items'].append({'section': 'penalty_items', 'condition': condition, 'score': -item_penalty})
                else:
                    detail['unrecognized_items'].append({'section': 'penalty_items', 'condition': condition, 'score': -item_penalty})
        
        return penalty

# 测试代码
if __name__ == '__main__':
    # 创建测试数据
    import pandas as pd
    from datetime import datetime, timedelta
    
    dates = pd.date_range(end=datetime.now(), periods=30)
    test_data = pd.DataFrame({
        '日期': dates,
        '开盘': range(100, 130),
        '最高': range(101, 131),
        '最低': range(99, 129),
        '收盘': range(100, 130),
        '成交量': range(1000, 1300, 10)
    })
    
    # 创建测试公式信息
    test_formula = {
        'trend_strength': {
            'max_score': 30,
            'items': [
                {'condition': 'MA5 > MA20', 'score': 10},
                {'condition': 'MA10 > MA30', 'score': 10},
                {'condition': '均线多头排列（MA5>MA10>MA20）', 'score': 5},
                {'condition': '近期5日涨幅 > 3%', 'score': 5}
            ]
        },
        'momentum_confirmation': {
            'max_score': 25,
            'items': [
                {'condition': 'MACD金叉且柱状图扩大', 'score': 10},
                {'condition': 'KDJ（K>D且在20-80区间）', 'score': 10},
                {'condition': '收盘价突破布林带中轨且带宽扩张', 'score': 5}
            ]
        },
        'volume_price_coordination': {
            'max_score': 20,
            'items': [
                {'condition': '成交量 > 20日均量1.3倍', 'score': 10},
                {'condition': '量比（当日/5日均量）>1.2且持续2天', 'score': 10}
            ]
        },
        'risk_control': {
            'max_score': 15,
            'items': [
                {'condition': '10日波动率 < 近期30日波动率中位数', 'score': 5},
                {'condition': '价格处于20日均线上方且偏离度<8%', 'score': 5},
                {'condition': 'RSI在40-60之间', 'score': 5}
            ]
        },
        'market_environment': {
            'max_score': 10,
            'items': [
                {'condition': '近20日市场（参考沪深300）处于上涨趋势（涨幅>3%），且个股相对强度（个股涨幅/行业涨幅）>1', 'score': 10},
                {'condition': '其他情况', 'score': 5}
            ]
        },
        'penalty_items': [
            {'condition': '出现长上影线（单日振幅>5%且收盘低于最高点2%）', 'penalty': 5},
            {'condition': '大宗交易折价率>3%', 'penalty': 3},
            {'condition': '行业指数近5日跌幅>5%', 'penalty': 5},
            {'condition': '涨幅>5%但波动率同步放大', 'penalty': 3},
            {'condition': '价涨量缩', 'penalty': 5},
            {'condition': 'RSI>70或<30', 'penalty': 5}
        ]
    }
    
    # 测试评分器
    scorer = DynamicScorer(test_formula)
    score = scorer.calculate_score(test_data)
    print(f"测试评分: {score}")
