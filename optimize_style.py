#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
优化项目样式
使用web_designer模块生成更好的样式并应用到项目中
"""

import os
import sys

# 添加当前目录到路径
sys.path.insert(0, os.path.dirname(__file__))

from web_designer import get_web_designer

def optimize_project_style():
    """
    优化项目样式
    """
    print("=" * 60)
    print("优化项目样式")
    print("=" * 60)
    
    # 获取网页设计师实例
    designer = get_web_designer()
    
    # 生成现代风格的颜色方案
    print("生成现代风格的颜色方案...")
    palette = designer.generate_color_palette("modern")
    print(f"颜色方案: {palette}")
    
    # 生成CSS样式
    print("生成CSS样式...")
    css_style = designer.generate_css_style(palette)
    print("CSS样式生成完成")
    
    # 读取当前的app.py文件
    app_path = os.path.join(os.path.dirname(__file__), "app.py")
    with open(app_path, "r", encoding="utf-8") as f:
        app_content = f.read()
    
    # 找到样式设置部分
    style_start = app_content.find("# 样式设置 - 高对比度版本")
    style_end = app_content.find("# ====================", style_start)
    
    if style_start != -1 and style_end != -1:
        # 替换样式设置
        new_style = f"# 样式设置 - 现代风格版本\n# 使用web_designer模块生成的优化样式\nst.markdown('''{css_style}''', unsafe_allow_html=True)"
        new_app_content = app_content[:style_start] + new_style + app_content[style_end:]
        
        # 保存修改
        with open(app_path, "w", encoding="utf-8") as f:
            f.write(new_app_content)
        
        print("项目样式优化完成！")
        print("新的样式已经应用到app.py文件中")
    else:
        print("未找到样式设置部分，无法优化")
    
    # 显示优化前后的对比
    print("\n" + "=" * 60)
    print("优化前后对比")
    print("=" * 60)
    print("优化前: 高对比度黑色主题")
    print("优化后: 现代风格主题")
    print(f"主色: {palette['primary']}")
    print(f"辅助色: {palette['secondary']}")
    print(f"背景色: {palette['background']}")
    print(f"表面色: {palette['surface']}")
    print(f"文字色: {palette['text']}")
    
    # 生成设计提示
    print("\n" + "=" * 60)
    print("网页设计提示")
    print("=" * 60)
    tips = designer.generate_web_design_tips()
    for i, tip in enumerate(tips, 1):
        print(f"{i}. {tip}")

if __name__ == "__main__":
    optimize_project_style()
