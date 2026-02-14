#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
腾讯财经数据源 - 备用数据接口
当东方财富不可用时使用
"""

import requests
import pandas as pd
import time
from typing import List, Dict

# 使用绝对导入的方式导入config模块
import sys
import os

# 添加主项目根目录到路径
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from config import get_proxies


class TencentDataSource:
    """腾讯财经数据接口"""
    
    def __init__(self):
        self.base_url = "https://qt.gtimg.cn/q="
        self.proxies = get_proxies()  # 使用配置文件中的代理设置
        self.headers = {
            'User-Agent': 'Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36'
        }
    
    def _parse_quote(self, text: str) -> List[Dict]:
        """解析腾讯行情数据"""
        stocks = []
        
        for line in text.split('\n'):
            if '=' not in line:
                continue
            
            try:
                # 解析 v_sh600000="..."
                parts = line.split('=')
                if len(parts) != 2:
                    continue
                
                code_part = parts[0].replace('v_', '').strip()
                data_str = parts[1].strip('"')
                data = data_str.split('~')
                
                if len(data) < 30:
                    continue
                
                # 提取股票代码
                if code_part.startswith('sh'):
                    code = code_part[2:]
                    market = '沪市'
                elif code_part.startswith('sz'):
                    code = code_part[2:]
                    market = '深市'
                else:
                    code = code_part
                    market = ''
                
                # 解析数据
                # 腾讯返回格式: 字段3=最新价, 字段4=涨跌额, 字段32=涨跌幅
                change_pct = 0
                change_amt = 0
                if len(data) > 32 and data[32]:
                    try:
                        change_pct = float(data[32])
                    except:
                        change_pct = 0
                if len(data) > 4 and data[4]:
                    try:
                        change_amt = float(data[4])
                    except:
                        change_amt = 0
                
                # 解析市盈率和市净率
                pe = 0
                pb = 0
                if len(data) > 39 and data[39]:
                    try:
                        pe = float(data[39])
                    except:
                        pe = 0
                if len(data) > 41 and data[41]:
                    try:
                        pb = float(data[41])
                    except:
                        pb = 0
                
                stocks.append({
                    '代码': code,
                    '名称': data[1] if len(data) > 1 else '',
                    '最新价': float(data[3]) if len(data) > 3 and data[3] and data[3] != '0.00' else 0,
                    '涨跌额': change_amt,
                    '成交量': float(data[5]) if len(data) > 5 and data[5] else 0,
                    '成交额': float(data[6]) * 100 if len(data) > 6 and data[6] else 0,  # 腾讯成交额单位是万元，需要乘100
                    '振幅': float(data[7]) if len(data) > 7 and data[7] else 0,
                    '最高': float(data[8]) if len(data) > 8 and data[8] else 0,
                    '最低': float(data[9]) if len(data) > 9 and data[9] else 0,
                    '今开': float(data[10]) if len(data) > 10 and data[10] else 0,
                    '昨收': float(data[11]) if len(data) > 11 and data[11] else 0,
                    '涨跌幅': change_pct,
                    '市盈率': pe,
                    '市净率': pb,
                    'Market': market,
                })
            except Exception as e:
                continue
        
        return stocks
    
    def get_realtime_quote(self, codes: List[str]) -> pd.DataFrame:
        """
        获取实时行情
        
        Args:
            codes: 股票代码列表，如 ['600000', '600036']
        
        Returns:
            DataFrame: 行情数据
        """
        # 转换为腾讯格式
        symbols = []
        for code in codes:
            if code.startswith('6'):
                symbols.append(f'sh{code}')
            elif code.startswith('0') or code.startswith('3'):
                symbols.append(f'sz{code}')
            else:
                symbols.append(f'sh{code}')
        
        # 批量获取（腾讯支持最多80个）
        url = self.base_url + ','.join(symbols[:80])
        
        try:
            resp = requests.get(url, headers=self.headers, 
                              proxies=self.proxies, timeout=15)
            
            if resp.status_code == 200:
                stocks = self._parse_quote(resp.text)
                return pd.DataFrame(stocks)
        except Exception as e:
            print(f"腾讯API错误: {e}")
        
        return pd.DataFrame()
    
    def get_realtime_quotes(self, count: int = 100) -> pd.DataFrame:
        """
        获取实时行情 - 通过热门股票列表
        
        Args:
            count: 获取数量
        
        Returns:
            DataFrame: 行情数据
        """
        # 常用股票代码列表 - 扩展覆盖更多
        common_stocks = [
            '600000', '600016', '600036', '600050', '600104', '600519', '600887',
            '601012', '601088', '601166', '601318', '601398', '601857', '601988',
            '000001', '000002', '000063', '000333', '000651', '000858',
            '002594', '002714', '300059', '300750', '300896'
        ]
        
        # 补充更多代码 - 扩展到全市场
        # 沪市A股 (6开头)
        for i in range(600000, 603000):
            common_stocks.append(f'6{i:05d}')
        for i in range(601000, 602000):
            common_stocks.append(f'6{i:05d}')
        
        # 深市主板 (0开头)
        for i in range(1, 1000):
            common_stocks.append(f'0{i:04d}')
        
        # 中小板 (002开头) - 包含三花智控002050
        for i in range(1, 1000):
            common_stocks.append(f'002{i:03d}')
        
        # 创业板 (3开头)
        for i in range(1, 1000):
            common_stocks.append(f'3{i:04d}')
        
        # 北交所 (8, 4开头)
        for i in range(1, 500):
            common_stocks.append(f'8{i:04d}')
            common_stocks.append(f'4{i:04d}')
        
        # 去重
        common_stocks = list(set(common_stocks))
        
        # 获取全部
        all_stocks = []
        for i in range(0, len(common_stocks), 80):
            batch = common_stocks[i:i+80]
            df = self.get_realtime_quote(batch)
            if len(df) > 0:
                all_stocks.append(df)
            # 添加延迟避免请求过快
            import time
            time.sleep(0.1)
        
        if all_stocks:
            result = pd.concat(all_stocks, ignore_index=True)
            # 按涨跌幅排序
            result = result.sort_values('涨跌幅', ascending=False)
            return result.head(count)
        
        return pd.DataFrame()
    
    def get_stock_kline(self, symbol: str, start_date: str = None, 
                        period: str = 'day') -> pd.DataFrame:
        """
        获取K线数据
        
        Args:
            symbol: 股票代码，如 '600000'
            start_date: 开始日期 'YYYYMMDD'（可选）
            period: 'day', 'week', 'month'
        
        Returns:
            DataFrame: K线数据
        """
        # 转换代码格式
        if symbol.startswith('6'):
            code = f'sh{symbol}'
        else:
            code = f'sz{symbol}'
        
        # 使用新的API接口
        url = f'http://data.gtimg.cn/flashdata/hushen/latest/daily/{code}.js'
        
        try:
            print(f"获取K线数据: {symbol} -> {code}")
            print(f"API URL: {url}")
            
            resp = requests.get(url, headers=self.headers,
                              proxies=self.proxies, timeout=15)
            
            print(f"响应状态码: {resp.status_code}")
            
            if resp.status_code == 200:
                text = resp.text
                print(f"响应数据长度: {len(text)}")
                
                # 提取数据部分
                if 'latest_daily_data="' in text:
                    # 提取数据并清理
                    data_part = text.split('latest_daily_data="')[1].strip('"')
                    # 清理可能的转义字符
                    data_part = data_part.replace('\\n', '\n')
                    data_part = data_part.replace('\\"', '"')
                    lines = data_part.split('\n')
                    
                    print(f"数据行数: {len(lines)}")
                    
                    klines = []
                    for line in lines:
                        line = line.strip()
                        if line and not line.startswith('num:') and not line.startswith('start:') and not line.startswith('total:'):
                            # 清理行尾可能的特殊字符
                            line = line.rstrip('\\')
                            parts = line.split()
                            if len(parts) >= 6:
                                # 解析K线数据
                                # 格式: 日期 收盘 开盘 最高 最低 成交量
                                try:
                                    date_str = parts[0]
                                    # 验证日期格式（6位数字）
                                    if len(date_str) != 6 or not date_str.isdigit():
                                        continue
                                    
                                    # 安全转换为浮点数
                                    close = float(parts[1])
                                    open_price = float(parts[2])
                                    high = float(parts[3])
                                    low = float(parts[4])
                                    volume = float(parts[5])
                                    
                                    # 转换日期格式
                                    # 格式: 220527 → 2022-05-27
                                    year = '20' + date_str[:2]
                                    month = date_str[2:4]
                                    day = date_str[4:6]
                                    date = f'{year}-{month}-{day}'
                                    
                                    klines.append({
                                        '日期': date,
                                        '开盘': open_price,
                                        '收盘': close,
                                        '最高': high,
                                        '最低': low,
                                        '成交量': volume,
                                        '成交额': 0  # 新API没有直接提供成交额
                                    })
                                except Exception as e:
                                    # 跳过解析失败的行
                                    print(f"解析K线数据失败: {e}, 行: {line}")
                                    continue
                    
                    print(f"解析出的K线数量: {len(klines)}")
                    
                    df = pd.DataFrame(klines)
                    if len(df) > 0:
                        df['日期'] = pd.to_datetime(df['日期'])
                        df.set_index('日期', inplace=True)
                        print(f"返回K线数据: {len(df)} 条")
                        return df
                    else:
                        print("解析出的K线数量为0")
                else:
                    print("响应数据中没有找到 latest_daily_data 字段")
            else:
                print(f"响应状态码错误: {resp.status_code}")
        except Exception as e:
            print(f"腾讯K线错误: {e}")
        
        print("返回空DataFrame")
        return pd.DataFrame()


def get_quotes(count=100) -> pd.DataFrame:
    """便捷函数"""
    source = TencentDataSource()
    return source.get_realtime_quotes(count)


def get_kline(symbol: str, start_date: str = None) -> pd.DataFrame:
    """便捷函数"""
    source = TencentDataSource()
    return source.get_stock_kline(symbol, start_date)


# 测试
if __name__ == '__main__':
    print("=" * 50)
    print("腾讯财经数据源测试")
    print("=" * 50)
    
    source = TencentDataSource()
    
    # 测试1: 单只股票
    print("\n1. 测试获取单只股票:")
    df = source.get_realtime_quote(['600000', '600036', '600519'])
    if len(df) > 0:
        print(df[['代码', '名称', '最新价', '涨跌幅']])
    
    # 测试2: K线
    print("\n2. 测试获取K线:")
    kline = source.get_stock_kline('600519')
    if len(kline) > 0:
        print(kline.tail())
    
    print("\n✅ 测试完成")
