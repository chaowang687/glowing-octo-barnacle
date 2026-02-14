#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
DeepSeek AI 个股分析模块
使用DeepSeek API进行智能股票分析
"""

import requests
import json
from typing import Dict, Optional
import logging

logger = logging.getLogger('a_quant')

class DeepSeekAnalyzer:
    """
    DeepSeek AI 个股分析器
    """
    
    def __init__(self, api_key: str = ""):
        """
        初始化DeepSeek分析器
        
        Args:
            api_key: DeepSeek API密钥
        """
        self.api_key = api_key
        self.base_url = "https://api.deepseek.com/v1/chat/completions"
        self.headers = {
            "Content-Type": "application/json",
            "Authorization": f"Bearer {api_key}"
        }
    
    def test_connection(self) -> Optional[str]:
        """
        测试DeepSeek API连接状态
        
        Returns:
            str: 连接状态信息
        """
        if not self.api_key:
            return "API密钥未设置"
        
        try:
            # 发送一个简单的测试请求
            test_payload = {
                "model": "deepseek-chat",
                "messages": [
                    {
                        "role": "user",
                        "content": "测试连接"
                    }
                ],
                "temperature": 0.7,
                "max_tokens": 10
            }
            
            response = requests.post(
                self.base_url,
                headers=self.headers,
                json=test_payload,
                timeout=10
            )
            
            if response.status_code == 200:
                return f"连接成功，状态码: {response.status_code}"
            else:
                return f"连接失败，状态码: {response.status_code}, 错误: {response.text}"
                
        except requests.exceptions.Timeout:
            return "连接超时"
        except requests.exceptions.ConnectionError:
            return "网络连接错误"
        except Exception as e:
            return f"连接失败: {str(e)}"
    
    def analyze_stock(self, stock_data: Dict, max_retries: int = 5) -> Optional[str]:
        """
        使用DeepSeek分析股票
        
        Args:
            stock_data: 股票数据字典，包含代码、名称、K线数据等
            max_retries: 最大重试次数
        
        Returns:
            str: AI分析结果
        """
        if not self.api_key:
            logger.warning("DeepSeek API密钥未设置")
            # 生成DeepSeek网页版链接
            prompt = self._build_analysis_prompt(stock_data)
            web_link = self._generate_deepseek_web_link(prompt)
            return f"请设置DeepSeek API密钥以使用AI分析功能\n\n或者使用DeepSeek网页版免费分析：\n[点击跳转到DeepSeek网页版分析]({web_link})"
        
        try:
            # 构建分析提示词
            prompt = self._build_analysis_prompt(stock_data)
            
            # 调用DeepSeek API（增强版重试机制）
            response = None
            for attempt in range(max_retries):
                logger.info(f"尝试调用DeepSeek API (尝试 {attempt + 1}/{max_retries})")
                
                # 先测试连接状态
                connection_status = self.test_connection()
                if "连接成功" in connection_status:
                    logger.info(f"API连接正常，开始分析...")
                else:
                    logger.warning(f"API连接状态: {connection_status}")
                    logger.info("等待网络连接恢复...")
                    import time
                    time.sleep(3)  # 等待3秒后重试
                    continue
                
                # 调用API
                response = self._call_deepseek_api(prompt)
                
                # 检查响应
                if response:
                    # 检查是否是错误信息
                    if "⚠️" not in response and "错误" not in response and "失败" not in response and "网络连接" not in response:
                        # 成功获取响应
                        logger.info("API调用成功，分析完成！")
                        return response
                    else:
                        logger.warning(f"API返回错误信息: {response[:100]}...")
                else:
                    logger.warning("API调用返回空响应")
                
                # 准备重试
                if attempt < max_retries - 1:
                    wait_time = min(2 ** attempt, 10)  # 最大等待10秒
                    logger.info(f"API调用失败，{wait_time}秒后重试...")
                    import time
                    time.sleep(wait_time)  # 指数退避
            
            # 所有重试都失败
            logger.error(f"所有{max_retries}次重试都失败")
            if response:
                return response
            else:
                # API调用失败，生成网页版链接
                web_link = self._generate_deepseek_web_link(prompt)
                return f"AI分析失败，请重试\n\n或者使用DeepSeek网页版免费分析：\n[点击跳转到DeepSeek网页版分析]({web_link})"
                
        except Exception as e:
            logger.error(f"DeepSeek分析错误: {e}")
            # 分析出错，生成网页版链接
            prompt = self._build_analysis_prompt(stock_data)
            web_link = self._generate_deepseek_web_link(prompt)
            return f"分析出错: {str(e)}\n\n建议使用DeepSeek网页版免费分析：\n[点击跳转到DeepSeek网页版分析]({web_link})"
    
    def _generate_deepseek_web_link(self, prompt: str) -> str:
        """
        生成DeepSeek网页版链接，包含预设提示词
        
        Args:
            prompt: 分析提示词
        
        Returns:
            str: DeepSeek网页版链接
        """
        import urllib.parse
        
        # DeepSeek网页版URL
        base_url = "https://chat.deepseek.com/"
        
        # 生成包含提示词的链接
        # 注意：DeepSeek网页版可能不支持直接通过URL传递提示词
        # 这里使用一个通用的方式，用户可以复制提示词到网页版
        
        # 对于支持URL参数的AI聊天网站，可以使用类似这样的格式：
        # encoded_prompt = urllib.parse.quote(prompt)
        # return f"{base_url}?prompt={encoded_prompt}"
        
        # 由于DeepSeek网页版可能不支持URL参数，我们返回基础URL
        # 并在提示中告知用户复制提示词
        return base_url
    
    def _build_analysis_prompt(self, stock_data: Dict) -> str:
        """
        构建分析提示词
        
        Args:
            stock_data: 股票数据
        
        Returns:
            str: 提示词
        """
        symbol = stock_data.get('symbol', '')
        name = stock_data.get('name', '')
        kline_data = stock_data.get('kline_data', None)
        market_analysis = stock_data.get('market_analysis', {})
        
        # 提取最近K线数据（完整版）
        recent_data = ""
        if kline_data is not None and len(kline_data) > 0:
            # 获取最近10天的数据
            recent_kline = kline_data.tail(10)
            recent_data = "最近10天K线数据:\n"
            for _, row in recent_kline.iterrows():
                date = str(row.get('日期', '')).split(' ')[0] if ' ' in str(row.get('日期', '')) else str(row.get('日期', ''))
                close = row.get('收盘', 0)
                change = row.get('涨跌幅', 0)
                volume = row.get('成交量', 0)
                recent_data += f"{date}: 收盘价={close:.2f}, 涨跌幅={change:.2f}%, 成交量={volume}\n"
        
        # 提取市场分析数据
        market_info = ""
        if market_analysis:
            factors = market_analysis.get('factors', {})
            if factors:
                bullish = factors.get('bullish', [])[:3]  # 只取前3条
                bearish = factors.get('bearish', [])[:3]  # 只取前3条
                if bullish:
                    market_info += "利好因素:\n" + "\n".join([f"- {item}" for item in bullish]) + "\n\n"
                if bearish:
                    market_info += "利空因素:\n" + "\n".join([f"- {item}" for item in bearish]) + "\n\n"
        
        prompt = f"""# A股市场分析任务

## 股票信息
- 代码：{symbol}
- 名称：{name}

## K线数据
{recent_data}

## 市场信息
{market_info}

## 分析要求
请作为资深A股分析师，提供以下分析：

1. **技术面分析**：K线形态、趋势方向、关键指标状态
2. **短期走势**：未来3-5天预测、支撑阻力位
3. **操作建议**：明确操作建议、仓位控制、止损止盈
4. **风险评估**：主要风险因素
5. **投资逻辑**：核心投资逻辑

## 输出格式
- 使用中文回答
- 直接进入分析，无需引言
- 保持简洁专业
- 使用Markdown格式
- 重点突出，条理清晰"""
        
        return prompt
    
    def _call_deepseek_api(self, prompt: str) -> Optional[str]:
        """
        调用DeepSeek API
        
        Args:
            prompt: 提示词
        
        Returns:
            str: API响应
        """
        payload = {
            "model": "deepseek-chat",
            "messages": [
                {
                    "role": "user",
                    "content": prompt
                }
            ],
            "temperature": 0.7,
            "max_tokens": 1000
        }
        
        try:
            logger.info(f"开始调用DeepSeek API")
            logger.info(f"API URL: {self.base_url}")
            logger.info(f"API密钥长度: {len(self.api_key)}")
            
            response = requests.post(
                self.base_url,
                headers=self.headers,
                json=payload,
                timeout=30
            )
            
            logger.info(f"API响应状态码: {response.status_code}")
            
            if response.status_code == 200:
                data = response.json()
                if 'choices' in data and data['choices']:
                    logger.info("API调用成功")
                    return data['choices'][0]['message']['content']
                else:
                    logger.error(f"API返回数据格式错误: {data}")
                    return f"API返回数据格式错误: {data}"
            else:
                error_message = f"DeepSeek API错误: {response.status_code} - {response.text}"
                logger.error(error_message)
                
                # 解析错误信息，提供更详细的提示
                if response.status_code == 402:
                    return f"⚠️ API余额不足: 您的DeepSeek API密钥余额已用完，请充值或使用网页版进行分析。\n\n[点击跳转到DeepSeek网页版](https://chat.deepseek.com/)"
                elif response.status_code == 401:
                    return f"⚠️ API密钥无效: 您的DeepSeek API密钥格式错误或已过期，请检查并重新输入。"
                elif response.status_code == 429:
                    return f"⚠️ API调用频率过高: 请稍后再试，或使用DeepSeek网页版进行分析。\n\n[点击跳转到DeepSeek网页版](https://chat.deepseek.com/)"
                else:
                    return f"⚠️ API调用失败: {response.status_code} - {response.text}\n\n建议使用DeepSeek网页版进行分析: [点击跳转](https://chat.deepseek.com/)"
                    
        except requests.exceptions.Timeout:
            error_message = "网络连接超时: 无法连接到DeepSeek API服务器，请检查网络连接。"
            logger.error(error_message)
            return f"⚠️ {error_message}\n\n建议使用DeepSeek网页版进行分析: [点击跳转](https://chat.deepseek.com/)"
        except requests.exceptions.ConnectionError:
            error_message = "网络连接错误: 无法连接到DeepSeek API服务器，请检查网络连接。"
            logger.error(error_message)
            return f"⚠️ {error_message}\n\n建议使用DeepSeek网页版进行分析: [点击跳转](https://chat.deepseek.com/)"
        except Exception as e:
            error_message = f"API调用失败: {str(e)}"
            logger.error(error_message)
            return f"⚠️ {error_message}\n\n建议使用DeepSeek网页版进行分析: [点击跳转](https://chat.deepseek.com/)"

def get_deepseek_analyzer(api_key: str = "") -> DeepSeekAnalyzer:
    """
    获取DeepSeek分析器实例
    
    Args:
        api_key: DeepSeek API密钥
    
    Returns:
        DeepSeekAnalyzer: 分析器实例
    """
    return DeepSeekAnalyzer(api_key)

# 测试
if __name__ == '__main__':
    import pandas as pd
    from datetime import datetime, timedelta
    
    # 创建测试数据
    dates = pd.date_range(end=datetime.now(), periods=10, freq='D')
    kline_data = pd.DataFrame({
        '日期': dates,
        '收盘': [100, 102, 105, 103, 106, 108, 110, 112, 115, 113],
        '涨跌幅': [0, 2, 2.94, -1.90, 2.91, 1.89, 1.85, 1.82, 2.68, -1.74],
        '成交量': [1000000, 1200000, 1500000, 1300000, 1400000, 1600000, 1800000, 2000000, 2200000, 1900000]
    })
    
    test_data = {
        'symbol': '600519',
        'name': '贵州茅台',
        'kline_data': kline_data
    }
    
    # 测试分析器
    analyzer = DeepSeekAnalyzer()
    result = analyzer.analyze_stock(test_data)
    print("AI分析结果:")
    print(result)
