#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Agent工具集模块
提供给AI助手使用的各种工具函数
"""

import pandas as pd
import json
from datetime import datetime, timedelta
from typing import Dict, List, Optional, Union, Any

from data_source import get_quotes, get_kline
from backtest_engine import BacktestParams, run_backtest
from dynamic_scorer import DynamicScorer
from score_formula_parser import ScoreFormulaParser

def get_market_snapshot(top_n: int = 20) -> str:
    """
    获取市场行情快照
    
    Args:
        top_n: 获取涨幅榜前N名
        
    Returns:
        str: 格式化的行情文本
    """
    try:
        df = get_quotes(top_n)
        if df.empty:
            return "无法获取市场行情数据"
            
        result = f"市场行情快照 ({datetime.now().strftime('%Y-%m-%d %H:%M')})\n"
        result += f"涨幅前{len(df)}名:\n"
        
        for _, row in df.iterrows():
            code = row['代码']
            name = row['名称']
            price = row['最新价']
            change = row['涨跌幅']
            result += f"- {code} {name}: {price} ({change}%)\n"
            
        return result
    except Exception as e:
        return f"获取行情失败: {str(e)}"

def check_stock_price(symbol: str) -> str:
    """
    查询个股当前价格
    
    Args:
        symbol: 股票代码
        
    Returns:
        str: 价格信息
    """
    try:
        # 这里复用get_quotes，虽然效率不高但兼容现有接口
        # 实际项目中应该有专门的个股查询接口
        df = get_quotes(5000) # 获取所有或大量
        stock = df[df['代码'] == symbol]
        
        if stock.empty:
            return f"未找到股票代码 {symbol} 的行情信息"
            
        row = stock.iloc[0]
        return f"股票: {row['代码']} {row['名称']}\n最新价: {row['最新价']}\n涨跌幅: {row['涨跌幅']}%\n成交量: {row['成交量']}"
    except Exception as e:
        return f"查询失败: {str(e)}"

def quick_backtest(symbol: str, buy_threshold: float = 60.0, hold_days: int = 5) -> str:
    """
    快速回测指定股票
    
    Args:
        symbol: 股票代码
        buy_threshold: 买入评分阈值
        hold_days: 持有天数
        
    Returns:
        str: 回测简报
    """
    try:
        # 获取K线
        kline = get_kline(symbol, period='101')
        if kline.empty:
            return f"无法获取 {symbol} 的K线数据"
            
        # 使用默认评分器
        parser = ScoreFormulaParser()
        # 这里简单使用默认公式，实际可以支持传入公式
        formula_info = parser.parse_deepseek_result("") 
        scorer = DynamicScorer(formula_info)
        
        # 参数
        params = BacktestParams(
            buy_threshold=buy_threshold,
            sell_threshold=buy_threshold-10, # 默认
            max_holding_days=hold_days,
            take_profit_pct=10.0,
            stop_loss_pct=-5.0
        )
        
        # 运行回测
        res = run_backtest(kline, scorer, params)
        
        metrics = res.metrics
        return json.dumps(metrics, ensure_ascii=False, indent=2)
        
    except Exception as e:
        return f"回测失败: {str(e)}"

# 工具定义列表，用于System Prompt
TOOLS_DESC = """
可用工具列表:
1. get_market_snapshot(top_n: int = 20) -> str
   描述: 获取当前市场涨幅榜前N名的股票行情。
   示例: "查看市场行情", "看看现在谁涨得好"

2. check_stock_price(symbol: str) -> str
   描述: 查询指定股票代码的当前价格和涨跌幅。
   示例: "查询 000001 的价格", "600519 现在多少钱"

3. quick_backtest(symbol: str, buy_threshold: float = 60.0, hold_days: int = 5) -> str
   描述: 对指定股票进行快速回测，返回年化收益、胜率等指标。
   示例: "回测 000001", "帮我测一下 600519，阈值设为50"

请根据用户需求选择合适的工具。如果需要调用工具，请按以下JSON格式输出（不要输出其他内容）：
```json
{
    "tool": "工具函数名",
    "params": {
        "参数名": "参数值"
    }
}
```
如果不需要调用工具，请直接回复用户。
"""

def dispatch_tool(tool_name: str, params: dict) -> str:
    """
    工具分发器
    """
    if tool_name == "get_market_snapshot":
        return get_market_snapshot(**params)
    elif tool_name == "check_stock_price":
        return check_stock_price(**params)
    elif tool_name == "quick_backtest":
        return quick_backtest(**params)
    else:
        return f"未知的工具: {tool_name}"
