#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
专业PDF文档生成模块 - Professional Report Style
用于生成专业金融报告风格的PDF文档
与网页界面风格保持一致
"""

from reportlab.lib import colors
from reportlab.lib.pagesizes import A4
from reportlab.lib.styles import getSampleStyleSheet, ParagraphStyle
from reportlab.lib.units import cm, mm
from reportlab.platypus import (SimpleDocTemplate, Paragraph, Spacer, Table, 
                                 TableStyle, Image, PageBreak, KeepTogether)
from reportlab.lib.enums import TA_CENTER, TA_LEFT, TA_RIGHT, TA_JUSTIFY
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.platypus.flowables import HRFlowable
from reportlab.pdfgen import canvas
import pandas as pd
from datetime import datetime
import os

class ProfessionalPDFGenerator:
    """
    专业PDF文档生成器
    采用专业金融报告风格设计
    """
    
    # 专业报告配色方案（与网页一致）
    COLORS = {
        'primary': colors.HexColor('#1E40AF'),      # 深蓝色 - 主色调
        'secondary': colors.HexColor('#D97706'),    # 金色 - 辅助色
        'accent': colors.HexColor('#059669'),       # 绿色 - 利好
        'danger': colors.HexColor('#DC2626'),       # 红色 - 利空
        'background': colors.HexColor('#F8FAFC'),   # 浅灰背景
        'surface': colors.white,                     # 白色表面
        'surface_dark': colors.HexColor('#F1F5F9'), # 深色表面
        'text': colors.HexColor('#0F172A'),         # 深色文字
        'text_light': colors.HexColor('#475569'),   # 浅色文字
        'border': colors.HexColor('#E2E8F0'),       # 边框色
        'border_light': colors.HexColor('#F1F5F9'), # 浅边框
    }
    
    def __init__(self):
        """初始化PDF生成器"""
        self.styles = getSampleStyleSheet()
        self._register_fonts()
        self._setup_professional_styles()
    
    def _register_fonts(self):
        """注册中文字体 - 确保中文正常显示"""
        try:
            # macOS系统中文字体路径（按优先级排序）
            font_configs = [
                # PingFang - macOS默认中文字体，支持完整中文
                ('PingFang', '/System/Library/Fonts/PingFang.ttc'),
                # STHeiti - macOS黑体
                ('STHeiti', '/System/Library/Fonts/STHeiti Medium.ttc'),
                # Arial Unicode - 备选方案
                ('ArialUnicode', '/System/Library/Fonts/Supplemental/Arial Unicode.ttf'),
            ]
            
            for font_name, font_path in font_configs:
                if os.path.exists(font_path):
                    try:
                        pdfmetrics.registerFont(TTFont(font_name, font_path))
                        # 注册Bold版本（使用同一字体）
                        pdfmetrics.registerFont(TTFont(f'{font_name}-Bold', font_path))
                        self.chinese_font = font_name
                        self.chinese_font_bold = f'{font_name}-Bold'
                        print(f"✅ 成功注册中文字体: {font_name} ({font_path})")
                        return
                    except Exception as e:
                        print(f"⚠️ 注册字体 {font_name} 失败: {e}")
                        continue
            
            # 如果所有字体都失败，使用Helvetica作为备选
            self.chinese_font = 'Helvetica'
            self.chinese_font_bold = 'Helvetica-Bold'
            print("⚠️ 未找到中文字体，使用Helvetica（中文可能显示为方块）")
            
        except Exception as e:
            print(f"❌ 字体注册失败: {e}")
            self.chinese_font = 'Helvetica'
            self.chinese_font_bold = 'Helvetica-Bold'
    
    def _setup_professional_styles(self):
        """设置专业报告样式"""
        
        # 确保有字体属性
        if not hasattr(self, 'chinese_font'):
            self.chinese_font = 'Helvetica'
            self.chinese_font_bold = 'Helvetica-Bold'
        
        # 主标题样式 - 大标题
        self.title_style = ParagraphStyle(
            'ProfessionalTitle',
            parent=self.styles['Heading1'],
            fontSize=28,
            textColor=self.COLORS['primary'],
            spaceAfter=8,
            spaceBefore=0,
            alignment=TA_CENTER,
            fontName=self.chinese_font_bold,
            leading=36
        )
        
        # 副标题样式
        self.subtitle_style = ParagraphStyle(
            'ProfessionalSubtitle',
            parent=self.styles['Heading2'],
            fontSize=14,
            textColor=self.COLORS['text_light'],
            spaceAfter=20,
            alignment=TA_CENTER,
            fontName=self.chinese_font,
            leading=20
        )

        # 章节标题样式
        self.section_title_style = ParagraphStyle(
            'SectionTitle',
            parent=self.styles['Heading2'],
            fontSize=16,
            textColor=self.COLORS['primary'],
            spaceAfter=12,
            spaceBefore=16,
            fontName=self.chinese_font_bold,
            leading=22,
            borderWidth=0,
            borderColor=self.COLORS['secondary'],
            borderPadding=8,
            backColor=self.COLORS['surface_dark']
        )
        
        # 小节标题样式
        self.subsection_title_style = ParagraphStyle(
            'SubsectionTitle',
            parent=self.styles['Heading3'],
            fontSize=13,
            textColor=self.COLORS['secondary'],
            spaceAfter=8,
            spaceBefore=10,
            fontName=self.chinese_font_bold,
            leading=18,
            leftIndent=8
        )
        
        # 正文样式
        self.body_style = ParagraphStyle(
            'ProfessionalBody',
            parent=self.styles['BodyText'],
            fontSize=11,
            textColor=self.COLORS['text'],
            leading=20,
            spaceAfter=6,
            fontName=self.chinese_font,
            alignment=TA_LEFT,
            leftIndent=12
        )
        
        # 强调文本样式
        self.emphasis_style = ParagraphStyle(
            'Emphasis',
            parent=self.body_style,
            fontSize=12,
            textColor=self.COLORS['primary'],
            fontName=self.chinese_font_bold
        )
        
        # 数据样式
        self.data_style = ParagraphStyle(
            'DataStyle',
            parent=self.body_style,
            fontSize=11,
            textColor=self.COLORS['text'],
            fontName='Courier'  # 等宽字体适合数据
        )
        
        # 页脚样式
        self.footer_style = ParagraphStyle(
            'Footer',
            parent=self.styles['Normal'],
            fontSize=9,
            textColor=self.COLORS['text_light'],
            alignment=TA_CENTER,
            fontName=self.chinese_font,
            leading=14
        )
        
        # 警告样式
        self.warning_style = ParagraphStyle(
            'Warning',
            parent=self.body_style,
            fontSize=10,
            textColor=self.COLORS['danger'],
            backColor=colors.HexColor('#FEE2E2'),
            borderWidth=1,
            borderColor=self.COLORS['danger'],
            borderPadding=8
        )
    
    def _create_header_footer(self, canvas, doc, stock_info):
        """创建专业的页眉页脚"""
        canvas.saveState()
        
        # 页眉 - 深蓝色背景条
        canvas.setFillColor(self.COLORS['primary'])
        canvas.rect(0, A4[1] - 2*cm, A4[0], 2*cm, fill=True, stroke=False)
        
        # 页眉文字
        canvas.setFillColor(colors.white)
        canvas.setFont('Helvetica-Bold', 16)
        canvas.drawString(2*cm, A4[1] - 1.2*cm, "A股量化选股系统")
        
        canvas.setFont('Helvetica', 10)
        canvas.drawString(2*cm, A4[1] - 1.6*cm, "Professional Stock Analysis Report")
        
        # 页眉右侧 - 股票信息
        if stock_info:
            canvas.setFont('Helvetica-Bold', 12)
            canvas.drawRightString(A4[0] - 2*cm, A4[1] - 1.2*cm, 
                                  f"{stock_info['symbol']} {stock_info['name']}")
            canvas.setFont('Helvetica', 9)
            canvas.drawRightString(A4[0] - 2*cm, A4[1] - 1.6*cm, 
                                  datetime.now().strftime('%Y-%m-%d %H:%M:%S'))
        
        # 页脚 - 浅灰色背景
        canvas.setFillColor(self.COLORS['surface_dark'])
        canvas.rect(0, 0, A4[0], 1.5*cm, fill=True, stroke=False)
        
        # 页脚文字
        canvas.setFillColor(self.COLORS['text_light'])
        canvas.setFont('Helvetica', 8)
        
        # 左侧 - 版权信息
        canvas.drawString(2*cm, 0.8*cm, "© 2026 A股量化选股系统")
        canvas.drawString(2*cm, 0.5*cm, "仅供参考，不构成投资建议")
        
        # 右侧 - 页码
        page_num = canvas.getPageNumber()
        canvas.drawRightString(A4[0] - 2*cm, 0.8*cm, f"第 {page_num} 页")
        canvas.drawRightString(A4[0] - 2*cm, 0.5*cm, "Professional Report")
        
        canvas.restoreState()
    
    def _create_professional_table(self, data, col_widths=None, header_bg=None):
        """创建专业样式的表格"""
        if not header_bg:
            # 使用渐变色效果（深蓝到稍浅蓝）
            header_bg = self.COLORS['primary']
        
        table = Table(data, colWidths=col_widths)
        
        table_style = TableStyle([
            # 表头样式 - 深蓝色背景
            ('BACKGROUND', (0, 0), (-1, 0), header_bg),
            ('TEXTCOLOR', (0, 0), (-1, 0), colors.white),
            ('ALIGN', (0, 0), (-1, 0), 'CENTER'),
            ('FONTNAME', (0, 0), (-1, 0), 'Helvetica-Bold'),
            ('FONTSIZE', (0, 0), (-1, 0), 11),
            ('BOTTOMPADDING', (0, 0), (-1, 0), 12),
            ('TOPPADDING', (0, 0), (-1, 0), 12),
            
            # 表格内容样式
            ('ALIGN', (0, 1), (-1, -1), 'CENTER'),
            ('FONTNAME', (0, 1), (-1, -1), 'Helvetica'),
            ('FONTSIZE', (0, 1), (-1, -1), 10),
            ('TOPPADDING', (0, 1), (-1, -1), 8),
            ('BOTTOMPADDING', (0, 1), (-1, -1), 8),
            
            # 斑马条纹效果
            ('ROWBACKGROUNDS', (0, 1), (-1, -1), 
             [self.COLORS['surface'], self.COLORS['surface_dark']]),
            
            # 边框样式
            ('GRID', (0, 0), (-1, -1), 0.5, self.COLORS['border']),
            ('BOX', (0, 0), (-1, -1), 1.5, self.COLORS['primary']),
            
            # 圆角效果（通过调整边框实现）
            ('VALIGN', (0, 0), (-1, -1), 'MIDDLE'),
        ])
        
        table.setStyle(table_style)
        return table
    
    def _create_section_divider(self):
        """创建章节分隔线"""
        return HRFlowable(
            width="100%",
            thickness=2,
            color=self.COLORS['border'],
            spaceBefore=10,
            spaceAfter=10,
            hAlign='CENTER',
            vAlign='BOTTOM',
            dash=None
        )
    
    def _create_info_box(self, title, content_list, box_type='info'):
        """创建信息框"""
        # 根据类型选择颜色
        box_colors = {
            'info': (self.COLORS['primary'], colors.HexColor('#DBEAFE')),
            'success': (self.COLORS['accent'], colors.HexColor('#D1FAE5')),
            'warning': (self.COLORS['secondary'], colors.HexColor('#FEF3C7')),
            'danger': (self.COLORS['danger'], colors.HexColor('#FEE2E2')),
        }
        
        border_color, bg_color = box_colors.get(box_type, box_colors['info'])
        
        # 创建表格作为信息框
        data = [[title]]
        for content in content_list:
            data.append([content])
        
        table = Table(data, colWidths=[17*cm])
        
        table.setStyle(TableStyle([
            # 标题行
            ('BACKGROUND', (0, 0), (-1, 0), border_color),
            ('TEXTCOLOR', (0, 0), (-1, 0), colors.white),
            ('FONTNAME', (0, 0), (-1, 0), 'Helvetica-Bold'),
            ('FONTSIZE', (0, 0), (-1, 0), 12),
            ('ALIGN', (0, 0), (-1, 0), 'LEFT'),
            ('LEFTPADDING', (0, 0), (-1, 0), 12),
            ('TOPPADDING', (0, 0), (-1, 0), 10),
            ('BOTTOMPADDING', (0, 0), (-1, 0), 10),
            
            # 内容行
            ('BACKGROUND', (0, 1), (-1, -1), bg_color),
            ('FONTNAME', (0, 1), (-1, -1), 'Helvetica'),
            ('FONTSIZE', (0, 1), (-1, -1), 10),
            ('ALIGN', (0, 1), (-1, -1), 'LEFT'),
            ('LEFTPADDING', (0, 1), (-1, -1), 12),
            ('TOPPADDING', (0, 1), (-1, -1), 6),
            ('BOTTOMPADDING', (0, 1), (-1, -1), 6),
            
            # 边框
            ('BOX', (0, 0), (-1, -1), 2, border_color),
            ('VALIGN', (0, 0), (-1, -1), 'TOP'),
        ]))
        
        return table
    
    def generate_analysis_report(self, stock_data, analysis_result, output_path):
        """
        生成专业风格的股票分析报告
        
        Args:
            stock_data: 股票数据字典
            analysis_result: AI分析结果
            output_path: 输出PDF文件路径
        
        Returns:
            bool: 是否生成成功
        """
        try:
            # 提取股票基本信息
            stock_info = {
                'symbol': stock_data.get('symbol', '未知'),
                'name': stock_data.get('name', '未知')
            }
            
            # 创建PDF文档，使用自定义页眉页脚
            doc = SimpleDocTemplate(
                output_path,
                pagesize=A4,
                rightMargin=2*cm,
                leftMargin=2*cm,
                topMargin=2.5*cm,
                bottomMargin=2*cm
            )
            
            # 内容列表
            story = []
            
            # ========== 封面设计 ==========
            story.append(Spacer(1, 3*cm))
            
            # 主标题
            title = f"股票分析报告"
            story.append(Paragraph(title, self.title_style))
            story.append(Spacer(1, 0.5*cm))
            
            # 股票信息
            stock_title = f"{stock_info['symbol']} {stock_info['name']}"
            story.append(Paragraph(stock_title, 
                                  ParagraphStyle('StockTitle',
                                               parent=self.title_style,
                                               fontSize=22,
                                               textColor=self.COLORS['secondary'])))

            story.append(Spacer(1, 2*cm))
            
            # 报告信息框
            report_info = [
                f"报告类型: 专业分析报告",
                f"生成时间: {datetime.now().strftime('%Y年%m月%d日 %H:%M:%S')}",
                f"分析工具: A股全能量化选股系统",
                f"数据来源: 腾讯财经 / 东方财富",
            ]
            story.append(self._create_info_box("报告信息", report_info, 'info'))
            
            story.append(PageBreak())
            
            # ========== 1. 基本信息 ==========
            story.append(Paragraph("1. 基本信息", self.section_title_style))
            story.append(self._create_section_divider())
            story.append(Spacer(1, 0.3*cm))
            
            basic_info = [
                f"股票代码: {stock_info['symbol']}",
                f"股票名称: {stock_info['name']}",
            ]
            
            # 计算基本统计数据
            kline_data = stock_data.get('kline_data')
            if kline_data is not None and len(kline_data) > 0:
                latest_data = kline_data.tail(1).iloc[0]
                latest_close = latest_data.get('收盘', 0)
                
                if len(kline_data) > 1:
                    prev_close = kline_data.tail(2).iloc[0].get('收盘', 0)
                    if prev_close > 0:
                        change = latest_close - prev_close
                        change_pct = (change / prev_close) * 100
                        
                        change_color = 'danger' if change < 0 else 'success'
                        basic_info.extend([
                            f"最新价: {latest_close:.2f} 元",
                            f"涨跌额: {change:+.2f} 元",
                            f"涨跌幅: {change_pct:+.2f}%"
                        ])
            
            story.append(self._create_info_box("股票基础数据", basic_info, 'info'))
            story.append(Spacer(1, 0.5*cm))
            
            # ========== 2. K线数据分析 ==========
            story.append(Paragraph("2. K线数据分析", self.section_title_style))
            story.append(self._create_section_divider())
            story.append(Spacer(1, 0.3*cm))
            
            if kline_data is not None and len(kline_data) > 0:
                # 最近10天K线数据表格
                recent_kline = kline_data.tail(10).copy()
                
                table_data = [['日期', '开盘', '收盘', '最高', '最低', '成交量']]
                for _, row in recent_kline.iterrows():
                    date = str(row.get('日期', '')).split(' ')[0]
                    open_price = f"{row.get('开盘', 0):.2f}"
                    close = f"{row.get('收盘', 0):.2f}"
                    high = f"{row.get('最高', 0):.2f}"
                    low = f"{row.get('最低', 0):.2f}"
                    volume = f"{row.get('成交量', 0):,.0f}"
                    table_data.append([date, open_price, close, high, low, volume])
                
                table = self._create_professional_table(
                    table_data, 
                    col_widths=[3*cm, 2.5*cm, 2.5*cm, 2.5*cm, 2.5*cm, 3*cm]
                )
                story.append(table)
                story.append(Spacer(1, 0.5*cm))
                
                # K线统计数据
                avg_price = recent_kline['收盘'].mean()
                max_price = recent_kline['最高'].max()
                min_price = recent_kline['最低'].min()
                avg_volume = recent_kline['成交量'].mean()
                
                stats_info = [
                    f"平均收盘价: {avg_price:.2f} 元",
                    f"期间最高价: {max_price:.2f} 元",
                    f"期间最低价: {min_price:.2f} 元",
                    f"平均成交量: {avg_volume:,.0f} 手",
                    f"价格波动: {((max_price - min_price) / min_price * 100):.2f}%"
                ]
                story.append(self._create_info_box("统计数据", stats_info, 'success'))
            else:
                story.append(Paragraph("暂无K线数据", self.body_style))
            
            story.append(Spacer(1, 0.5*cm))
            
            # ========== 3. 市场信息分析 ==========
            story.append(Paragraph("3. 市场信息分析", self.section_title_style))
            story.append(self._create_section_divider())
            story.append(Spacer(1, 0.3*cm))
            
            market_analysis = stock_data.get('market_analysis', {})
            
            if market_analysis:
                factors = market_analysis.get('factors', {})
                
                # 利好因素
                bullish = factors.get('bullish', [])
                if bullish:
                    bullish_info = [f"• {factor}" for factor in bullish[:5]]
                    story.append(self._create_info_box("利好因素", bullish_info, 'success'))
                    story.append(Spacer(1, 0.3*cm))
                
                # 利空因素
                bearish = factors.get('bearish', [])
                if bearish:
                    bearish_info = [f"• {factor}" for factor in bearish[:5]]
                    story.append(self._create_info_box("利空因素", bearish_info, 'danger'))
                    story.append(Spacer(1, 0.3*cm))
                
                # 行业热点
                hotspots = factors.get('industry_hotspots', [])
                if hotspots:
                    hotspots_info = [f"• {hotspot}" for hotspot in hotspots[:3]]
                    story.append(self._create_info_box("行业热点", hotspots_info, 'warning'))
            else:
                story.append(Paragraph("暂无市场信息数据", self.body_style))
            
            story.append(Spacer(1, 0.5*cm))
            
            # ========== 4. AI分析结果 ==========
            if analysis_result and "错误" not in analysis_result:
                story.append(Paragraph("4. AI智能分析", self.section_title_style))
                story.append(self._create_section_divider())
                story.append(Spacer(1, 0.3*cm))
                
                # 将分析结果分段显示
                analysis_paragraphs = analysis_result.split('\n\n')
                for para in analysis_paragraphs:
                    if para.strip():
                        story.append(Paragraph(para.strip(), self.body_style))
                        story.append(Spacer(1, 0.2*cm))
            
            # ========== 5. 风险提示 ==========
            story.append(Paragraph("5. 风险提示", self.section_title_style))
            story.append(self._create_section_divider())
            story.append(Spacer(1, 0.3*cm))
            
            risk_warnings = [
                "⚠️ 市场风险: 股市有风险，投资需谨慎",
                "⚠️ 数据风险: 本报告基于公开数据，可能存在延迟或错误",
                "⚠️ 分析风险: 量化分析仅供参考，不构成投资建议",
                "⚠️ 操作风险: 投资决策应综合考虑个人风险承受能力",
                "⚠️ 信息风险: 请以官方公告和实时行情为准"
            ]
            story.append(self._create_info_box("重要提示", risk_warnings, 'danger'))
            
            # ========== 页脚信息 ==========
            story.append(Spacer(1, 2*cm))
            story.append(self._create_section_divider())
            story.append(Spacer(1, 0.5*cm))
            
            footer_text = [
                "本报告由 A股全能量化选股系统 自动生成",
                "融合缠论结构 · CPV量价分析 · 基本面筛选 · AI智能分析",
                "© 2026 A-Quant System · Professional Stock Analysis Platform",
                "Powered by DeepSeek AI · TencentFinance Data · EastMoney API"
            ]
            
            for text in footer_text:
                story.append(Paragraph(text, self.footer_style))
            
            # 生成PDF，应用页眉页脚
            doc.build(story, onFirstPage=lambda c, d: self._create_header_footer(c, d, stock_info),
                     onLaterPages=lambda c, d: self._create_header_footer(c, d, stock_info))
            
            return True
            
        except Exception as e:
            print(f"❌ 生成专业PDF失败: {e}")
            import traceback
            traceback.print_exc()
            return False

# 便捷函数
def generate_professional_pdf_report(stock_data, analysis_result, output_dir='./reports'):
    """
    生成专业风格PDF报告的便捷函数
    
    Args:
        stock_data: 股票数据字典
        analysis_result: AI分析结果
        output_dir: 输出目录
    
    Returns:
        str: 生成的PDF文件路径
    """
    os.makedirs(output_dir, exist_ok=True)
    
    symbol = stock_data.get('symbol', 'unknown')
    name = stock_data.get('name', 'unknown')
    timestamp = datetime.now().strftime('%Y%m%d_%H%M%S')
    filename = f"{symbol}_{name}_专业报告_{timestamp}.pdf"
    output_path = os.path.join(output_dir, filename)
    
    generator = ProfessionalPDFGenerator()
    success = generator.generate_analysis_report(stock_data, analysis_result, output_path)
    
    return output_path if success else None
