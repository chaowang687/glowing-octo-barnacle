# -*- coding: utf-8 -*-
"""
数据解析模块
负责清洗、解析和标准化从各数据源获取的原始数据
"""

import re
import json
from datetime import datetime
from typing import Optional, Dict, List, Any
from bs4 import BeautifulSoup

from utils import logger


class DataParser:
    """数据解析器基类"""
    
    @staticmethod
    def clean_html(text: str) -> str:
        """
        清洗HTML标签
        
        Args:
            text: 原始文本
            
        Returns:
            清洗后的文本
        """
        if not text:
            return ""
        
        # 移除HTML标签
        soup = BeautifulSoup(text, 'lxml')
        clean_text = soup.get_text()
        
        # 移除多余空白字符
        clean_text = re.sub(r'\s+', ' ', clean_text).strip()
        
        return clean_text
    
    @staticmethod
    def clean_stock_code(code: str) -> str:
        """
        清洗股票代码
        
        Args:
            code: 原始代码
            
        Returns:
            标准化后的6位股票代码
        """
        if not code:
            return ""
        
        # 提取数字部分
        numbers = re.findall(r'\d+', str(code))
        if numbers:
            return numbers[0].zfill(6)
        return ""
    
    @staticmethod
    def parse_datetime(date_str: str, fmt: str = "%Y-%m-%d %H:%M:%S") -> Optional[datetime]:
        """
        解析日期时间字符串
        
        Args:
            date_str: 日期字符串
            fmt: 日期格式
            
        Returns:
            datetime对象，解析失败返回None
        """
        if not date_str:
            return None
        
        try:
            return datetime.strptime(date_str, fmt)
        except ValueError:
            # 尝试其他常见格式
            formats = [
                "%Y-%m-%d",
                "%Y/%m/%d",
                "%Y%m%d",
                "%Y-%m-%d %H:%M",
                "%Y-%m-%dT%H:%M:%S",
            ]
            for f in formats:
                try:
                    return datetime.strptime(date_str, f)
                except ValueError:
                    continue
            
            logger.warning(f"无法解析日期: {date_str}")
            return None


class NewsParser(DataParser):
    """新闻数据解析器"""
    
    @staticmethod
    def parse_eastmoney_news(data: Dict, keyword: str = "") -> List[Dict]:
        """
        解析东方财富新闻API响应
        
        Args:
            data: API响应数据
            keyword: 搜索关键词
            
        Returns:
            标准化后的新闻列表
        """
        news_list = []
        
        try:
            # 尝试解析JSONP响应
            if isinstance(data, str):
                # 提取JSON部分
                json_match = re.search(r'\((.*)\)', data)
                if json_match:
                    data = json.loads(json_match.group(1))
            
            # 获取新闻列表 - cmsArticle is directly an array
            articles = data.get('result', {}).get('cmsArticle', [])
            
            for item in articles:
                try:
                    news = {
                        'stock_code': NewsParser._extract_stock_code(item, keyword),
                        'stock_name': NewsParser._extract_stock_name(item),
                        'title': NewsParser.clean_html(item.get('title', '')),
                        'content': NewsParser.clean_html(item.get('content', '')),
                        'pub_date': NewsParser._normalize_date(item.get('date', '')),
                        'source': item.get('source', '东方财富'),
                        'url': item.get('url', ''),
                    }
                    
                    # 过滤空标题
                    if news['title']:
                        news_list.append(news)
                except Exception as e:
                    logger.warning(f"解析单条新闻失败: {e}")
                    continue
                    
        except Exception as e:
            logger.error(f"解析新闻数据失败: {e}")
        
        logger.info(f"成功解析 {len(news_list)} 条新闻")
        return news_list
    
    @staticmethod
    def _extract_stock_code(item: Dict, keyword: str) -> str:
        """从新闻项中提取股票代码"""
        # 优先使用关键词中的代码
        if keyword and keyword.isdigit() and len(keyword) == 6:
            return keyword
        
        # 尝试从内容中提取
        content = item.get('content', '') + item.get('title', '')
        codes = re.findall(r'(?:股票代码|代码)?[（\(]?(\d{6})[）\)]?', content)
        if codes:
            return codes[0]
        
        return ""
    
    @staticmethod
    def _extract_stock_name(item: Dict) -> str:
        """从新闻项中提取股票名称"""
        title = item.get('title', '')
        # 尝试匹配常见的股票名称模式
        names = re.findall(r'【(.+?)】', title)
        if names:
            return names[0]
        
        # 从content中提取
        content = item.get('content', '')
        names = re.findall(r'[\u4e00-\u9fa5]{2,6}(?:股份|集团|有限|公司|科技|实业)', content)
        if names:
            return names[0]
        
        return ""
    
    @staticmethod
    def _normalize_date(date_str: str) -> str:
        """标准化日期格式"""
        dt = NewsParser.parse_datetime(date_str)
        if dt:
            return dt.strftime("%Y-%m-%d %H:%M:%S")
        return ""


class FundParser(DataParser):
    """资金流向数据解析器"""
    
    @staticmethod
    def parse_eastmoney_funds(data: Dict, stock_code: str = "") -> List[Dict]:
        """
        解析东方财富资金流向API响应
        
        Args:
            data: API响应数据
            stock_code: 股票代码
            
        Returns:
            标准化后的资金流向列表
        """
        funds_list = []
        
        try:
            klines = data.get('data', {}).get('klines', [])
            
            for kline in klines:
                # 格式: 日期,主力净流入,小单净流入,中单净流入,大单净流入,超大单净流入,...
                parts = kline.split(',')
                
                if len(parts) >= 6:
                    fund = {
                        'stock_code': stock_code,
                        'trade_date': parts[0],
                        'main_net_inflow': FundParser._parse_number(parts[1]),
                        'main_net_inflow_ratio': FundParser._parse_number(parts[2]) if len(parts) > 2 else 0,
                        'retail_net_inflow': FundParser._parse_number(parts[5]) if len(parts) > 5 else 0,
                        'super_net_inflow': FundParser._parse_number(parts[4]) if len(parts) > 4 else 0,
                    }
                    funds_list.append(fund)
                    
        except Exception as e:
            logger.error(f"解析资金流向数据失败: {e}")
        
        logger.info(f"成功解析 {len(funds_list)} 条资金流向数据")
        return funds_list
    
    @staticmethod
    def _parse_number(value: str) -> float:
        """解析数值，处理千分位和单位"""
        if not value:
            return 0.0
        
        try:
            # 移除逗号
            value = value.replace(',', '')
            
            # 处理单位
            multiplier = 1
            if '万' in value:
                multiplier = 10000
                value = value.replace('万', '')
            elif '亿' in value:
                multiplier = 100000000
                value = value.replace('亿', '')
            
            return float(value) * multiplier
            
        except (ValueError, AttributeError):
            return 0.0


class SectorParser(DataParser):
    """行业板块数据解析器"""
    
    @staticmethod
    def parse_eastmoney_sectors(data: Dict, trade_date: str = "") -> List[Dict]:
        """
        解析东方财富行业板块API响应
        
        Args:
            data: API响应数据
            trade_date: 交易日期
            
        Returns:
            标准化后的行业板块列表
        """
        sectors_list = []
        
        try:
            diff = data.get('data', {}).get('diff', [])
            
            for item in diff:
                # 东方财富字段映射: f12=代码, f13=市场, f14=名称, f2=价格, f3=涨跌幅
                sector_name = item.get('f14', '')
                
                # 只保留行业板块（过滤无效名称）
                if not sector_name:
                    continue
                
                sector = {
                    'sector_name': sector_name,
                    'trade_date': trade_date or datetime.now().strftime("%Y-%m-%d"),
                    'net_inflow': 0,  # 板块列表不直接提供净流入
                    'change_percent': SectorParser._parse_float(str(item.get('f3', '0'))),
                    'turnover_rate': 0,  # 板块列表不直接提供换手率
                }
                sectors_list.append(sector)
                
        except Exception as e:
            logger.error(f"解析行业板块数据失败: {e}")
        
        # 按净流入排序
        sectors_list.sort(key=lambda x: x['net_inflow'], reverse=True)
        
        logger.info(f"成功解析 {len(sectors_list)} 个行业板块")
        return sectors_list
    
    @staticmethod
    def _parse_number(value: str) -> float:
        """解析数值"""
        if not value:
            return 0.0
        try:
            return float(value.replace(',', ''))
        except (ValueError, AttributeError):
            return 0.0
    
    @staticmethod
    def _parse_float(value: str) -> float:
        """解析浮点数"""
        if not value:
            return 0.0
        try:
            return float(value)
        except (ValueError, AttributeError):
            return 0.0


class EventParser(DataParser):
    """事件数据解析器"""
    
    @staticmethod
    def parse_events(news_list: List[Dict]) -> List[Dict]:
        """
        从新闻列表中提取事件信息
        
        Args:
            news_list: 新闻列表
            
        Returns:
            事件列表
        """
        events_list = []
        
        # 事件类型关键词映射
        event_keywords = {
            '业绩预告': ['业绩预告', '业绩预增', '业绩预亏', '业绩预减'],
            '分红送转': ['分红', '送转', '增减持', '回购'],
            '重组并购': ['重组', '并购', '收购', '定增'],
            '高管变动': ['辞职', '上任', '离职', '任命'],
            '风险警示': ['ST', '*ST', '退市', '风险提示'],
            '中标': ['中标', '签约', '合同'],
        }
        
        for news in news_list:
            title = news.get('title', '')
            content = news.get('content', '')
            
            # 判断事件类型
            event_type = "其他"
            for etype, keywords in event_keywords.items():
                for kw in keywords:
                    if kw in title or kw in content:
                        event_type = etype
                        break
                if event_type != "其他":
                    break
            
            # 只有识别到特定类型才保存为事件
            if event_type != "其他":
                event = {
                    'stock_code': news.get('stock_code', ''),
                    'stock_name': news.get('stock_name', ''),
                    'event_type': event_type,
                    'event_date': news.get('pub_date', ''),
                    'event_title': title[:200],
                    'event_content': content[:500],
                    'source': news.get('source', ''),
                }
                events_list.append(event)
        
        logger.info(f"从 {len(news_list)} 条新闻中提取 {len(events_list)} 条事件")
        return events_list
