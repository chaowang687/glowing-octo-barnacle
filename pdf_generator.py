#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
PDF文档生成模块
用于生成股票分析报告的PDF文档
"""

from reportlab.lib import colors
from reportlab.lib.pagesizes import A4
from reportlab.lib.styles import getSampleStyleSheet, ParagraphStyle
from reportlab.lib.units import cm
from reportlab.platypus import SimpleDocTemplate, Paragraph, Spacer, Table, TableStyle, Image
from reportlab.lib.enums import TA_CENTER, TA_LEFT
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
import pandas as pd
from datetime import datetime
import os

class PDFGenerator:
    """
    PDF文档生成器
    """
    
    def __init__(self):
        """
        初始化PDF生成器
        """
        # 注意：为了确保中文字符能够正确显示，
        # 我们需要确保所有文本都使用Unicode编码
        self.styles = getSampleStyleSheet()
        self._register_fonts()
        self._setup_styles()
    
    def _register_fonts(self):
        """
        注册中文字体
        """
        try:
            # 方案1: 尝试使用系统中可用的中文字体
            font_configs = [
                # macOS系统中的Arial Unicode字体
                ('Arial Unicode MS', '/System/Library/Fonts/Supplemental/Arial Unicode.ttf'),
                # Windows系统中的SimHei字体
                ('SimHei', 'c:/windows/fonts/simhei.ttf'),
                # 其他常见路径
                ('SimHei', '/usr/share/fonts/truetype/SimHei.ttf'),
                ('WenQuanYi Micro Hei', '/usr/share/fonts/truetype/wqy/wqy-microhei.ttc'),
                ('Heiti TC', '/Library/Fonts/Heiti TC.ttc')
            ]
            
            for font_name, font_path in font_configs:
                try:
                    if os.path.exists(font_path):
                        pdfmetrics.registerFont(TTFont(font_name, font_path))
                        self.chinese_font = font_name
                        print(f"成功注册中文字体: {font_name} ({font_path})")
                        return
                    else:
                        print(f"字体文件不存在: {font_path}")
                except Exception as e:
                    print(f"注册字体 {font_name} 失败: {e}")
            
            # 方案2: 尝试直接使用字体名称（系统已安装的字体）
            system_fonts = ['SimHei', 'WenQuanYi Micro Hei', 'Heiti TC', 'Arial Unicode MS']
            for font_name in system_fonts:
                try:
                    pdfmetrics.registerFont(TTFont(font_name, font_name))
                    self.chinese_font = font_name
                    print(f"成功注册系统字体: {font_name}")
                    return
                except Exception as e:
                    print(f"注册系统字体 {font_name} 失败: {e}")
            
            # 方案3: 使用ReportLab内置的支持Unicode的字体
            try:
                # 检查是否有支持Unicode的内置字体
                available_fonts = pdfmetrics.getRegisteredFontNames()
                unicode_fonts = [font for font in available_fonts if 'unicode' in font.lower() or 'helvetica' in font.lower()]
                
                if unicode_fonts:
                    self.chinese_font = unicode_fonts[0]
                    print(f"使用内置Unicode字体: {self.chinese_font}")
                    return
                else:
                    # 使用Helvetica作为最后备选
                    self.chinese_font = 'Helvetica'
                    print("使用Helvetica作为最后备选字体")
                    return
            except Exception as e:
                print(f"使用内置字体失败: {e}")
            
            # 方案4: 最终备选方案
            self.chinese_font = 'Helvetica'
            print("未找到合适的字体，使用默认Helvetica字体")
        except Exception as e:
            print(f"字体注册失败: {e}")
            self.chinese_font = 'Helvetica'
    
    def _setup_styles(self):
        """
        设置PDF样式
        """
        # 确保有中文字体属性
        if not hasattr(self, 'chinese_font'):
            self.chinese_font = 'Helvetica'
        
        # 标题样式
        self.title_style = ParagraphStyle(
            'CustomTitle',
            parent=self.styles['Heading1'],
            fontSize=24,
            textColor=colors.HexColor('#333333'),
            spaceAfter=20,
            alignment=TA_CENTER,
            fontName=self.chinese_font
        )
        
        # 副标题样式
        self.subtitle_style = ParagraphStyle(
            'CustomSubtitle',
            parent=self.styles['Heading2'],
            fontSize=16,
            textColor=colors.HexColor('#555555'),
            spaceAfter=15,
            fontName=self.chinese_font
        )
        
        # 正文样式
        self.body_style = ParagraphStyle(
            'CustomBody',
            parent=self.styles['BodyText'],
            fontSize=11,
            textColor=colors.HexColor('#333333'),
            leading=18,
            spaceAfter=10,
            fontName=self.chinese_font
        )
        
        # 表格标题样式
        self.table_title_style = ParagraphStyle(
            'CustomTableTitle',
            parent=self.styles['Heading3'],
            fontSize=12,
            textColor=colors.HexColor('#444444'),
            spaceAfter=10,
            fontName=self.chinese_font
        )
    
    def generate_analysis_report(self, stock_data, analysis_result, output_path):
        """
        生成股票分析报告PDF
        
        Args:
            stock_data: 股票数据字典
            analysis_result: AI分析结果
            output_path: 输出PDF文件路径
        
        Returns:
            bool: 是否生成成功
        """
        try:
            # 创建PDF文档
            doc = SimpleDocTemplate(
                output_path,
                pagesize=A4,
                rightMargin=2*cm,
                leftMargin=2*cm,
                topMargin=2*cm,
                bottomMargin=2*cm
            )
            
            # 内容列表
            story = []
            
            # 添加标题（使用中文）
            title = f"股票分析报告: {stock_data['symbol']} ({stock_data['name']})"
            story.append(Paragraph(title, self.title_style))
            
            # 添加生成时间
            generate_time = datetime.now().strftime('%Y-%m-%d %H:%M:%S')
            story.append(Paragraph(f"生成时间: {generate_time}", self.body_style))
            story.append(Paragraph("报告类型: 专业分析", self.body_style))
            story.append(Spacer(1, 20))
            
            # 添加股票基本信息（使用中文）
            story.append(Paragraph("1. 基本信息", self.subtitle_style))
            story.append(Paragraph(f"• 股票代码: {stock_data.get('symbol', '未知')}", self.body_style))
            story.append(Paragraph(f"• 股票名称: {stock_data.get('name', '未知')}", self.body_style))
            
            # 计算基本统计数据
            kline_data = stock_data.get('kline_data')
            if kline_data is not None and len(kline_data) > 0:
                # 获取最新数据
                latest_data = kline_data.tail(1).iloc[0]
                latest_close = latest_data.get('收盘', 0)
                
                # 计算涨跌幅
                if len(kline_data) > 1:
                    prev_close = kline_data.tail(2).iloc[0].get('收盘', 0)
                    if prev_close > 0:
                        change_pct = ((latest_close - prev_close) / prev_close) * 100
                        story.append(Paragraph(f"• Latest Price: {latest_close:.2f}", self.body_style))
                        story.append(Paragraph(f"• Change: {change_pct:.2f}%", self.body_style))
            
            story.append(Spacer(1, 15))
            
            # 添加市场概况（使用中文）
            story.append(Paragraph("2. 市场概况", self.subtitle_style))
            story.append(Paragraph("• 数据来源: 腾讯财经", self.body_style))
            story.append(Paragraph("• 分析工具: A股量化选股系统", self.body_style))
            story.append(Paragraph("• 分析方法: 技术分析 + 量化指标", self.body_style))
            story.append(Spacer(1, 15))
            
            # 添加K线数据详细分析（使用中文）
            story.append(Paragraph("3. K线数据分析", self.subtitle_style))
            
            if kline_data is not None and len(kline_data) > 0:
                # 获取最近10天的K线数据
                recent_kline = kline_data.tail(10).copy()  # 使用copy()创建副本，避免修改原始数据
                
                # 准备表格数据（使用中文表头）
                table_data = [['序号', '日期', '开盘', '收盘', '最高', '最低', '成交量']]
                for i, (_, row) in enumerate(recent_kline.iterrows(), 1):
                    date = str(row.get('日期', '')).split(' ')[0]
                    open_price = f"{row.get('开盘', 0):.2f}"
                    close = f"{row.get('收盘', 0):.2f}"
                    high = f"{row.get('最高', 0):.2f}"
                    low = f"{row.get('最低', 0):.2f}"
                    volume = f"{row.get('成交量', 0):,}"
                    table_data.append([str(i), date, open_price, close, high, low, volume])
                
                # 创建表格
                table = Table(table_data, colWidths=[1*cm, 2.5*cm, 1.8*cm, 1.8*cm, 1.8*cm, 1.8*cm, 3*cm])
                
                # 设置表格样式
                table.setStyle(TableStyle([
                    ('BACKGROUND', (0, 0), (-1, 0), colors.HexColor('#336699')),
                    ('TEXTCOLOR', (0, 0), (-1, 0), colors.white),
                    ('ALIGN', (0, 0), (-1, -1), 'CENTER'),
                    ('FONTNAME', (0, 0), (-1, 0), self.chinese_font),
                    ('FONTNAME', (0, 1), (-1, -1), self.chinese_font),
                    ('FONTSIZE', (0, 0), (-1, 0), 10),
                    ('FONTSIZE', (0, 1), (-1, -1), 9),
                    ('BOTTOMPADDING', (0, 0), (-1, 0), 12),
                    ('GRID', (0, 0), (-1, -1), 1, colors.HexColor('#dddddd')),
                    ('ROWBACKGROUNDS', (0, 1), (-1, -1), [colors.HexColor('#f9f9f9'), colors.white]),
                ]))
                
                story.append(table)
                
                # 添加K线数据分析（使用中文）
                story.append(Spacer(1, 15))
                story.append(Paragraph("K线分析:", self.table_title_style))
                
                # 计算统计数据
                avg_price = recent_kline['收盘'].mean()
                max_price = recent_kline['最高'].max()
                min_price = recent_kline['最低'].min()
                avg_volume = recent_kline['成交量'].mean()
                total_volume = recent_kline['成交量'].sum()
                
                story.append(Paragraph(f"• 平均收盘价: {avg_price:.2f}", self.body_style))
                story.append(Paragraph(f"• 最高价: {max_price:.2f}", self.body_style))
                story.append(Paragraph(f"• 最低价: {min_price:.2f}", self.body_style))
                story.append(Paragraph(f"• 平均成交量: {avg_volume:,.0f}", self.body_style))
                story.append(Paragraph(f"• 总成交量: {total_volume:,.0f}", self.body_style))
            else:
                story.append(Paragraph("无K线数据可用", self.body_style))
            
            # 添加市场信息分析（使用中文）
            story.append(Spacer(1, 20))
            story.append(Paragraph("4. 市场信息分析", self.subtitle_style))
            
            # 检查是否有市场信息分析数据
            market_analysis = stock_data.get('market_analysis', {})
            
            if market_analysis:
                # 显示利好利空因素
                factors = market_analysis.get('factors', {})
                bullish = factors.get('bullish', [])
                bearish = factors.get('bearish', [])
                
                story.append(Paragraph("利好因素:", self.table_title_style))
                if bullish:
                    for factor in bullish[:3]:  # 显示前3条
                        story.append(Paragraph(f"• {factor}", self.body_style))
                else:
                    story.append(Paragraph("• 暂无明显利好因素", self.body_style))
                
                story.append(Paragraph("利空因素:", self.table_title_style))
                if bearish:
                    for factor in bearish[:3]:  # 显示前3条
                        story.append(Paragraph(f"• {factor}", self.body_style))
                else:
                    story.append(Paragraph("• 暂无明显利空因素", self.body_style))
                
                # 显示行业热点
                industry_hotspots = factors.get('industry_hotspots', [])
                story.append(Paragraph("行业热点:", self.table_title_style))
                if industry_hotspots:
                    for hotspot in industry_hotspots[:3]:  # 显示前3条
                        story.append(Paragraph(f"• {hotspot}", self.body_style))
                else:
                    story.append(Paragraph("• 暂无行业热点信息", self.body_style))
                
                # 显示市场趋势
                market_trends = factors.get('market_trends', [])
                story.append(Paragraph("市场趋势:", self.table_title_style))
                if market_trends:
                    for trend in market_trends:
                        story.append(Paragraph(f"• {trend}", self.body_style))
                else:
                    story.append(Paragraph("• 暂无市场趋势信息", self.body_style))
                
                # 显示主力资金状态
                main_funds = market_analysis.get('main_funds', {})
                if main_funds:
                    net_inflow = main_funds.get('net_inflow', 0)
                    status = main_funds.get('status', 'unknown')
                    
                    story.append(Paragraph("主力资金流向:", self.table_title_style))
                    story.append(Paragraph(f"• 净流入: {net_inflow/10000:.2f}万", self.body_style))
                    
                    status_text = {
                        'inflow': '流入',
                        'outflow': '流出',
                        'balanced': '平衡'
                    }.get(status, '未知')
                    story.append(Paragraph(f"• 状态: {status_text}", self.body_style))
                else:
                    story.append(Paragraph("• 主力资金数据不可用", self.body_style))
            else:
                story.append(Paragraph("• 市场信息不可用", self.body_style))
            
            # 添加投资建议（使用中文）
            story.append(Spacer(1, 20))
            story.append(Paragraph("5. 投资建议", self.subtitle_style))
            
            if kline_data is not None and len(kline_data) > 0:
                # 基于K线数据生成简单的投资建议（使用中文）
                last_close = kline_data['收盘'].iloc[-1]
                if len(kline_data) > 5:
                    ma5 = kline_data['收盘'].tail(5).mean()
                    ma10 = kline_data['收盘'].tail(10).mean()
                    
                    if last_close > ma5 > ma10:
                        advice = "短期看涨"
                        strategy = "考虑分批买入，设置合理止损"
                    elif last_close < ma5 < ma10:
                        advice = "短期看跌"
                        strategy = "规避风险，等待企稳信号"
                    else:
                        advice = "横盘整理"
                        strategy = "等待明确方向"
                else:
                    advice = "数据不足"
                    strategy = "等待更多数据再做决策"
                
                story.append(Paragraph(f"• 市场观点: {advice}", self.body_style))
                story.append(Paragraph(f"• 策略建议: {strategy}", self.body_style))
            else:
                story.append(Paragraph("• 市场观点: 数据不足", self.body_style))
                story.append(Paragraph("• 策略建议: 等待更多数据", self.body_style))
            
            # 添加风险提示（使用中文）
            story.append(Spacer(1, 20))
            story.append(Paragraph("6. 风险提示", self.subtitle_style))
            story.append(Paragraph("• 市场风险: 股市有风险，投资需谨慎", self.body_style))
            story.append(Paragraph("• 数据风险: 本报告基于公开数据，可能存在延迟或错误", self.body_style))
            story.append(Paragraph("• 分析风险: 量化分析仅供参考，不构成投资建议", self.body_style))
            story.append(Paragraph("• 操作风险: 投资决策应考虑个人风险承受能力", self.body_style))
            
            # 添加页脚（使用中文）
            story.append(Spacer(1, 30))
            footer_style = ParagraphStyle(
                'CustomFooter',
                parent=self.styles['Normal'],
                fontSize=9,
                textColor=colors.HexColor('#666666'),
                alignment=TA_CENTER,
                spaceAfter=10,
                fontName=self.chinese_font
            )
            story.append(Paragraph("本报告由A股量化选股系统自动生成，仅供参考，不构成投资建议。", footer_style))
            story.append(Paragraph("详细分析请使用DeepSeek网页版: https://chat.deepseek.com/", footer_style))
            story.append(Paragraph("© A股量化选股系统 2026", footer_style))
            
            # 生成PDF
            doc.build(story)
            
            return True
            
        except Exception as e:
            print(f"生成PDF失败: {e}")
            return False

def generate_pdf_report(stock_data, analysis_result, output_dir='./reports'):
    """
    生成PDF报告的便捷函数
    
    Args:
        stock_data: 股票数据字典
        analysis_result: AI分析结果
        output_dir: 输出目录
    
    Returns:
        str: 生成的PDF文件路径
    """
    # 确保输出目录存在
    os.makedirs(output_dir, exist_ok=True)
    
    # 生成文件名
    symbol = stock_data.get('symbol', 'unknown')
    name = stock_data.get('name', 'unknown')
    timestamp = datetime.now().strftime('%Y%m%d_%H%M%S')
    filename = f"{symbol}_{name}_{timestamp}.pdf"
    output_path = os.path.join(output_dir, filename)
    
    # 生成PDF
    generator = PDFGenerator()
    success = generator.generate_analysis_report(stock_data, analysis_result, output_path)
    
    if success:
        return output_path
    else:
        return None

def generate_selection_pdf_report(report_data, output_dir='./reports'):
    """
    生成选股报告的PDF文件
    
    Args:
        report_data: 报告数据字典，包含策略、参数、股票列表等
        output_dir: 输出目录
    
    Returns:
        str: 生成的PDF文件路径
    """
    # 确保输出目录存在
    os.makedirs(output_dir, exist_ok=True)
    
    # 生成文件名
    strategy = report_data.get('strategy', 'unknown')
    timestamp = datetime.now().strftime('%Y%m%d_%H%M%S')
    filename = f"selection_{strategy}_{timestamp}.pdf"
    output_path = os.path.join(output_dir, filename)
    
    # 创建PDF生成器
    generator = PDFGenerator()
    
    # 创建PDF文档
    doc = SimpleDocTemplate(
        output_path,
        pagesize=A4,
        rightMargin=2*cm,
        leftMargin=2*cm,
        topMargin=2*cm,
        bottomMargin=2*cm
    )
    
    # 内容列表
    story = []
    
    # 添加标题
    title = f"选股报告: {strategy}"
    story.append(Paragraph(title, generator.title_style))
    
    # 添加生成时间
    generate_time = datetime.now().strftime('%Y-%m-%d %H:%M:%S')
    story.append(Paragraph(f"生成时间: {generate_time}", generator.body_style))
    story.append(Paragraph(f"报告类型: 智能选股", generator.body_style))
    story.append(Spacer(1, 20))
    
    # 添加选股策略
    story.append(Paragraph("1. 选股策略", generator.subtitle_style))
    story.append(Paragraph(f"• 策略: {strategy}", generator.body_style))
    story.append(Paragraph(f"• 选股数量: {report_data.get('total_count', 0)}", generator.body_style))
    story.append(Paragraph(f"• 分析日期: {report_data.get('date', '未知')}", generator.body_style))
    story.append(Spacer(1, 15))
    
    # 添加选股参数
    story.append(Paragraph("2. 选股参数", generator.subtitle_style))
    params = report_data.get('params', {})
    
    # 技术指标参数
    story.append(Paragraph("技术指标:", generator.table_title_style))
    story.append(Paragraph(f"• 最低均线评分: {params.get('min_ma_score', 'N/A')}", generator.body_style))
    story.append(Paragraph(f"• 最低MACD评分: {params.get('min_macd_score', 'N/A')}", generator.body_style))
    story.append(Paragraph(f"• 最低KDJ评分: {params.get('min_kdj_score', 'N/A')}", generator.body_style))
    story.append(Paragraph(f"• 最低OBV评分: {params.get('min_obv_score', 'N/A')}", generator.body_style))
    
    # 基本面参数
    story.append(Paragraph("基本面指标:", generator.table_title_style))
    story.append(Paragraph(f"• 最大市盈率: {params.get('max_pe', 'N/A')}", generator.body_style))
    story.append(Paragraph(f"• 最低ROE (%): {params.get('min_roe', 'N/A')}", generator.body_style))
    story.append(Paragraph(f"• 最低市值 (亿): {params.get('min_market_cap', 'N/A')}", generator.body_style))
    story.append(Paragraph(f"• 最大负债率 (%): {params.get('max_debt_ratio', 'N/A')}", generator.body_style))
    story.append(Paragraph(f"• 选择行业: {params.get('sector', 'N/A')}", generator.body_style))
    story.append(Spacer(1, 15))
    
    # 添加选股结果
    story.append(Paragraph("3. 选股结果", generator.subtitle_style))
    
    stocks = report_data.get('stocks', [])
    if stocks:
        # 准备表格数据
        table_data = [['排名', '代码', '名称', '最新价', '涨跌幅', '成交量']]
        
        for i, stock in enumerate(stocks, 1):
            code = stock.get('代码', '')
            name = stock.get('名称', '')
            price = f"{stock.get('最新价', 0):.2f}"
            change = f"{stock.get('涨跌幅', 0):.2f}%"
            volume = f"{stock.get('成交量', 0):,}"
            
            table_data.append([str(i), code, name, price, change, volume])
        
        # 创建表格
        table = Table(table_data, colWidths=[1*cm, 2*cm, 3*cm, 2*cm, 2*cm, 3*cm])
        
        # 设置表格样式
        table.setStyle(TableStyle([
            ('BACKGROUND', (0, 0), (-1, 0), colors.HexColor('#336699')),
            ('TEXTCOLOR', (0, 0), (-1, 0), colors.white),
            ('ALIGN', (0, 0), (-1, -1), 'CENTER'),
            ('FONTNAME', (0, 0), (-1, 0), generator.chinese_font),
            ('FONTNAME', (0, 1), (-1, -1), generator.chinese_font),
            ('FONTSIZE', (0, 0), (-1, 0), 10),
            ('FONTSIZE', (0, 1), (-1, -1), 9),
            ('BOTTOMPADDING', (0, 0), (-1, 0), 12),
            ('GRID', (0, 0), (-1, -1), 1, colors.HexColor('#dddddd')),
            ('ROWBACKGROUNDS', (0, 1), (-1, -1), [colors.HexColor('#f9f9f9'), colors.white]),
        ]))
        
        story.append(table)
    else:
        story.append(Paragraph("未选出股票", generator.body_style))
    
    # 添加投资建议
    story.append(Spacer(1, 20))
    story.append(Paragraph("4. 投资建议", generator.subtitle_style))
    
    if stocks:
        story.append(Paragraph("• 分散投资: 考虑在不同行业间分散投资", generator.body_style))
        story.append(Paragraph("• 风险控制: 为每只股票设置合理的止损位", generator.body_style))
        story.append(Paragraph("• 定期监控: 定期监控所选股票的变化", generator.body_style))
        story.append(Paragraph("• 入场时机: 根据技术信号寻找最佳入场点", generator.body_style))
    else:
        story.append(Paragraph("• 未选出股票，考虑调整参数", generator.body_style))
        story.append(Paragraph("• 尝试不同的策略组合", generator.body_style))
        story.append(Paragraph("• 考虑更广泛的行业选择", generator.body_style))
    
    # 添加风险提示
    story.append(Spacer(1, 20))
    story.append(Paragraph("5. 风险提示", generator.subtitle_style))
    story.append(Paragraph("• 市场风险: 股市有风险，投资需谨慎", generator.body_style))
    story.append(Paragraph("• 数据风险: 本报告基于公开数据，可能存在延迟或错误", generator.body_style))
    story.append(Paragraph("• 分析风险: 量化分析仅供参考，不构成投资建议", generator.body_style))
    story.append(Paragraph("• 操作风险: 投资决策应考虑个人风险承受能力", generator.body_style))
    story.append(Paragraph("• 策略风险: 过往业绩不代表未来表现", generator.body_style))
    
    # 添加页脚
    story.append(Spacer(1, 30))
    footer_style = ParagraphStyle(
        'CustomFooter',
        parent=generator.styles['Normal'],
        fontSize=9,
        textColor=colors.HexColor('#666666'),
        alignment=TA_CENTER,
        spaceAfter=10,
        fontName=generator.chinese_font
    )
    story.append(Paragraph("本报告由A股量化选股系统自动生成，仅供参考，不构成投资建议。", footer_style))
    story.append(Paragraph("© A股量化选股系统 2026", footer_style))
    
    # 生成PDF
    try:
        doc.build(story)
        return output_path
    except Exception as e:
        print(f"生成选股PDF失败: {e}")
        return None
