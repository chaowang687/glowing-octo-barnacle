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
import tempfile

WATERMARK_TEXT = "glowing-octo-barnacle"

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
            base_dir = os.path.dirname(os.path.abspath(__file__))
            bundled_fonts = [
                ('SourceHanSansSC', os.path.join(base_dir, 'cardGame', 'Assets', 'TextMesh Pro', 'Fonts', 'SourceHanSansSC-Regular.ttf')),
                ('SourceHanSansCN', os.path.join(base_dir, 'cardGame', 'Assets', 'TextMesh Pro', 'Fonts', 'SourceHanSansCN-Normal.ttf')),
            ]

            for font_name, font_path in bundled_fonts:
                if os.path.exists(font_path):
                    pdfmetrics.registerFont(TTFont(font_name, font_path))
                    pdfmetrics.registerFont(TTFont(f'{font_name}-Bold', font_path))
                    self.chinese_font = font_name
                    self.chinese_font_bold = f'{font_name}-Bold'
                    print(f"✅ 成功注册中文字体: {font_name} ({font_path})")
                    return

            font_configs = [
                ('PingFang', '/System/Library/Fonts/PingFang.ttc'),
                ('STHeiti', '/System/Library/Fonts/STHeiti Medium.ttc'),
                ('ArialUnicode', '/System/Library/Fonts/Supplemental/Arial Unicode.ttf'),
                ('SimHei', 'C:\\Windows\\Fonts\\simhei.ttf'),
                ('SimSun', 'C:\\Windows\\Fonts\\simsun.ttc'),
                ('MicrosoftYaHei', 'C:\\Windows\\Fonts\\msyh.ttf'),
                ('WenQuanYi', '/usr/share/fonts/truetype/wqy/wqy-microhei.ttc'),
                ('NotoSans', '/usr/share/fonts/truetype/noto/NotoSansCJK-Regular.ttc'),
                ('ArialUnicode', '/Library/Fonts/Arial Unicode.ttf'),
            ]
            
            # 尝试注册字体
            for font_name, font_path in font_configs:
                if os.path.exists(font_path):
                    try:
                        if font_path.lower().endswith(('.ttc', '.otc')):
                            pdfmetrics.registerFont(TTFont(font_name, font_path, subfontIndex=0))
                            pdfmetrics.registerFont(TTFont(f'{font_name}-Bold', font_path, subfontIndex=0))
                        else:
                            pdfmetrics.registerFont(TTFont(font_name, font_path))
                            pdfmetrics.registerFont(TTFont(f'{font_name}-Bold', font_path))
                        # 注册Bold版本（使用同一字体）
                        self.chinese_font = font_name
                        self.chinese_font_bold = f'{font_name}-Bold'
                        print(f"✅ 成功注册中文字体: {font_name} ({font_path})")
                        return
                    except Exception as e:
                        print(f"⚠️ 注册字体 {font_name} 失败: {e}")
                        continue
            
            # 如果所有字体都失败，尝试使用reportlab内置字体
            # 检查是否有可用的中文字体
            try:
                # 尝试使用Courier作为备选，至少可以显示英文
                self.chinese_font = 'Courier'
                self.chinese_font_bold = 'Courier-Bold'
                print("⚠️ 未找到中文字体，使用Courier（英文正常，中文可能显示为方块）")
            except:
                # 最后的最后，使用默认字体
                self.chinese_font = 'Helvetica'
                self.chinese_font_bold = 'Helvetica-Bold'
                print("⚠️ 未找到任何字体，使用Helvetica（中文可能显示为方块）")
            
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
        canvas.setFont(self.chinese_font_bold, 16)
        canvas.drawString(2*cm, A4[1] - 1.2*cm, "A股量化选股系统")
        
        canvas.setFont(self.chinese_font, 10)
        canvas.drawString(2*cm, A4[1] - 1.6*cm, "Professional Stock Analysis Report")
        
        # 页眉右侧 - 股票信息
        if stock_info:
            canvas.setFont(self.chinese_font_bold, 12)
            canvas.drawRightString(A4[0] - 2*cm, A4[1] - 1.2*cm, 
                                  f"{stock_info['symbol']} {stock_info['name']}")
            canvas.setFont(self.chinese_font, 9)
            canvas.drawRightString(A4[0] - 2*cm, A4[1] - 1.6*cm, 
                                  datetime.now().strftime('%Y-%m-%d %H:%M:%S'))
        
        # 页脚 - 浅灰色背景
        canvas.setFillColor(self.COLORS['surface_dark'])
        canvas.rect(0, 0, A4[0], 1.5*cm, fill=True, stroke=False)
        
        # 页脚文字
        canvas.setFillColor(self.COLORS['text_light'])
        canvas.setFont(self.chinese_font, 8)
        
        # 左侧 - 版权信息
        canvas.drawString(2*cm, 0.8*cm, "© 2026 A股量化选股系统")
        canvas.drawString(2*cm, 0.5*cm, "仅供参考，不构成投资建议")
        
        # 右侧 - 页码
        page_num = canvas.getPageNumber()
        canvas.drawRightString(A4[0] - 2*cm, 0.8*cm, f"第 {page_num} 页")
        canvas.drawRightString(A4[0] - 2*cm, 0.5*cm, "Professional Report")
        
        canvas.restoreState()

    def _draw_watermark(self, canvas, doc):
        canvas.saveState()
        try:
            canvas.setFillAlpha(0.10)
        except Exception:
            pass
        canvas.setFillColor(colors.HexColor('#CBD5E1'))
        canvas.setFont(self.chinese_font_bold, 54)
        width, height = A4
        canvas.translate(width / 2, height / 2)
        canvas.rotate(30)
        canvas.drawCentredString(0, 0, WATERMARK_TEXT)
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
            ('FONTNAME', (0, 0), (-1, 0), self.chinese_font_bold),
            ('FONTSIZE', (0, 0), (-1, 0), 11),
            ('BOTTOMPADDING', (0, 0), (-1, 0), 12),
            ('TOPPADDING', (0, 0), (-1, 0), 12),
            
            # 表格内容样式
            ('ALIGN', (0, 1), (-1, -1), 'CENTER'),
            ('FONTNAME', (0, 1), (-1, -1), self.chinese_font),
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
            ('FONTNAME', (0, 0), (-1, 0), self.chinese_font_bold),
            ('FONTSIZE', (0, 0), (-1, 0), 12),
            ('ALIGN', (0, 0), (-1, 0), 'LEFT'),
            ('LEFTPADDING', (0, 0), (-1, 0), 12),
            ('TOPPADDING', (0, 0), (-1, 0), 10),
            ('BOTTOMPADDING', (0, 0), (-1, 0), 10),
            
            # 内容行
            ('BACKGROUND', (0, 1), (-1, -1), bg_color),
            ('FONTNAME', (0, 1), (-1, -1), self.chinese_font),
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
                for idx, row in recent_kline.iterrows():
                    raw_date = row.get('日期', None)
                    if raw_date is None or (isinstance(raw_date, float) and pd.isna(raw_date)) or str(raw_date) in ('', 'NaT', 'None'):
                        raw_date = idx
                    if isinstance(raw_date, (pd.Timestamp, datetime)):
                        date = raw_date.strftime('%Y-%m-%d')
                    else:
                        date = str(raw_date).split(' ')[0]
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
            def on_page(c, d):
                self._create_header_footer(c, d, stock_info)
                self._draw_watermark(c, d)

            doc.build(story, onFirstPage=on_page, onLaterPages=on_page)
            
            return True
            
        except Exception as e:
            print(f"❌ 生成专业PDF失败: {e}")
            import traceback
            traceback.print_exc()
            return False

    def generate_backtest_report(self, backtest_data, output_path):
        """
        生成回测报告PDF
        
        Args:
            backtest_data: 回测数据字典，包含:
                - stock_info: 股票信息 {symbol, name}
                - metrics: 指标 {total_trades, win_rate, avg_return, sharpe_ratio, max_drawdown, ...}
                - trades: 交易记录列表
                - formula: 使用的评分公式
                - eval_records: 评分预测力验证数据 (optional)
            output_path: 输出路径
            
        Returns:
            bool: 是否成功
        """
        try:
            stock_info = backtest_data.get('stock_info', {})
            metrics = backtest_data.get('metrics', {})
            trades = backtest_data.get('trades', [])
            formula = backtest_data.get('formula', '')
            equity_curves = backtest_data.get('equity_curves')
            comparison_metrics = backtest_data.get('comparison_metrics')
            
            # 创建PDF文档
            doc = SimpleDocTemplate(
                output_path,
                pagesize=A4,
                rightMargin=2*cm,
                leftMargin=2*cm,
                topMargin=2.5*cm,
                bottomMargin=2*cm
            )
            
            story = []
            
            # 封面
            story.append(Spacer(1, 3*cm))
            story.append(Paragraph("策略回测报告", self.title_style))
            story.append(Spacer(1, 0.5*cm))
            
            stock_title = f"{stock_info.get('symbol', '')} {stock_info.get('name', '')}"
            story.append(Paragraph(stock_title, 
                                  ParagraphStyle('StockTitle',
                                               parent=self.title_style,
                                               fontSize=22,
                                               textColor=self.COLORS['secondary'])))
            story.append(Spacer(1, 2*cm))
            
            report_info = [
                f"回测时间: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}",
                f"策略类型: AI评分模型 + T+1交易",
                f"初始资金: {metrics.get('initial_capital', 0):,.2f}",
                f"最终资金: {metrics.get('final_capital', 0):,.2f}"
            ]
            story.append(self._create_info_box("报告概览", report_info, 'info'))
            story.append(PageBreak())
            
            # 1. 核心绩效指标
            story.append(Paragraph("1. 核心绩效指标", self.section_title_style))
            story.append(self._create_section_divider())
            story.append(Spacer(1, 0.3*cm))
            
            perf_info = [
                f"总收益率: {metrics.get('total_return', 0):.2f}%",
                f"年化收益: {metrics.get('annual_return', 0):.2f}%",
                f"夏普比率: {metrics.get('sharpe_ratio', 0):.2f}",
                f"最大回撤: {metrics.get('max_drawdown', 0):.2f}%",
                f"盈亏比: {metrics.get('profit_loss_ratio', 0):.2f}",
                f"胜率: {metrics.get('win_rate', 0):.2f}% ({metrics.get('profitable_trades', 0)}/{metrics.get('total_trades', 0)})"
            ]
            story.append(self._create_info_box("绩效统计", perf_info, 'success' if metrics.get('total_return', 0) > 0 else 'danger'))
            story.append(Spacer(1, 0.5*cm))

            if comparison_metrics:
                story.append(Paragraph("2. 基线对比", self.section_title_style))
                story.append(self._create_section_divider())
                story.append(Spacer(1, 0.3*cm))

                try:
                    df = pd.DataFrame(comparison_metrics)
                    cols = ['方案', '总收益率%', '年化收益%', '夏普', '最大回撤%']
                    df = df[[c for c in cols if c in df.columns]].copy()
                    table_data = [df.columns.tolist()] + df.values.tolist()
                    table = self._create_professional_table(
                        table_data,
                        col_widths=[4*cm, 3*cm, 3*cm, 3*cm, 3*cm]
                    )
                    story.append(table)
                    story.append(Spacer(1, 0.5*cm))
                except Exception:
                    pass

            if equity_curves:
                story.append(Paragraph("3. 资金曲线对比", self.section_title_style))
                story.append(self._create_section_divider())
                story.append(Spacer(1, 0.3*cm))

                try:
                    import matplotlib.pyplot as plt

                    df = pd.DataFrame(equity_curves)
                    if '日期' in df.columns:
                        df['日期'] = pd.to_datetime(df['日期'])
                        df = df.sort_values('日期').set_index('日期')
                    df = df[[c for c in df.columns if c != '日期']]
                    df = df.fillna(method='ffill')

                    fig = plt.figure(figsize=(8.0, 3.2), dpi=200)
                    ax = fig.add_subplot(111)
                    for col in df.columns[:4]:
                        ax.plot(df.index, df[col], label=str(col), linewidth=1.6)
                    ax.set_title("Equity Curve Comparison")
                    ax.legend(loc='best', fontsize=8)
                    ax.grid(True, alpha=0.3)
                    fig.autofmt_xdate()

                    with tempfile.NamedTemporaryFile(suffix='.png', delete=False) as tmp:
                        fig.savefig(tmp.name, bbox_inches='tight')
                        img_path = tmp.name
                    plt.close(fig)

                    story.append(Image(img_path, width=17*cm, height=6.5*cm))
                    story.append(Spacer(1, 0.5*cm))
                except Exception:
                    pass
            
            story.append(Paragraph("4. 评分模型", self.section_title_style))
            story.append(self._create_section_divider())
            story.append(Spacer(1, 0.3*cm))
            
            if formula:
                # 简单处理公式文本，避免太长
                # 这里假设公式是markdown格式，简单转为纯文本展示
                formula_lines = formula.split('\n')
                for line in formula_lines:
                    if line.strip():
                        story.append(Paragraph(line.strip(), self.body_style))
            else:
                story.append(Paragraph("使用默认评分模型", self.body_style))
            
            story.append(Spacer(1, 0.5*cm))
            
            story.append(Paragraph("5. 交易记录详情", self.section_title_style))
            story.append(self._create_section_divider())
            story.append(Spacer(1, 0.3*cm))
            
            if trades:
                table_data = [['日期', '信号', '价格', '收益率', '资金余额']]
                for t in trades:
                    date_str = t['date'].strftime('%Y-%m-%d') if hasattr(t['date'], 'strftime') else str(t['date'])
                    signal = t.get('signal', '')
                    price = f"{t.get('price', 0):.2f}"
                    ret = f"{t.get('return', 0):.2f}%"
                    cap = f"{t.get('capital', 0):,.0f}"
                    table_data.append([date_str, signal, price, ret, cap])
                
                # 限制表格长度，如果太长
                if len(table_data) > 50:
                    table_data = table_data[:50]
                    story.append(Paragraph(f"* 仅显示前50笔交易，共 {len(trades)} 笔", self.body_style))
                
                table = self._create_professional_table(
                    table_data,
                    col_widths=[3*cm, 4*cm, 2.5*cm, 2.5*cm, 3.5*cm]
                )
                story.append(table)
            else:
                story.append(Paragraph("无交易记录", self.body_style))
                
            # 生成PDF
            def on_page(c, d):
                self._create_header_footer(c, d, stock_info)
                self._draw_watermark(c, d)

            doc.build(story, onFirstPage=on_page, onLaterPages=on_page)
            return True
            
        except Exception as e:
            print(f"❌ 生成回测报告PDF失败: {e}")
            import traceback
            traceback.print_exc()
            return False

# 便捷函数
def generate_professional_pdf_report(stock_data, analysis_result, output_dir='./reports'):
    """
    生成专业风格PDF报告的便捷函数
    ... (原代码不变)
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

def generate_backtest_pdf_report(backtest_data, output_dir='./reports'):
    """
    生成回测报告PDF的便捷函数
    """
    os.makedirs(output_dir, exist_ok=True)
    
    stock_info = backtest_data.get('stock_info', {})
    symbol = stock_info.get('symbol', 'unknown')
    name = stock_info.get('name', 'unknown')
    timestamp = datetime.now().strftime('%Y%m%d_%H%M%S')
    filename = f"{symbol}_{name}_回测报告_{timestamp}.pdf"
    output_path = os.path.join(output_dir, filename)
    
    generator = ProfessionalPDFGenerator()
    success = generator.generate_backtest_report(backtest_data, output_path)
    
    return output_path if success else None
