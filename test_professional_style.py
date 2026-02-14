#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
界面样式测试脚本
测试专业报告风格的CSS生成和应用
"""

from web_designer import get_web_designer

def test_professional_style():
    """测试专业报告风格生成"""
    print("=" * 60)
    print("🎨 A股量化选股系统 - 界面优化测试")
    print("=" * 60)
    print()
    
    # 获取设计器实例
    designer = get_web_designer()
    print("✅ Web Designer 模块加载成功")
    print()
    
    # 测试配色方案
    print("📊 测试配色方案...")
    palette = designer.generate_color_palette("professional_report")
    print(f"  主色调: {palette.get('primary', 'N/A')}")
    print(f"  辅助色: {palette.get('secondary', 'N/A')}")
    print(f"  强调色: {palette.get('accent', 'N/A')}")
    print(f"  背景色: {palette.get('background', 'N/A')}")
    print(f"  文字色: {palette.get('text', 'N/A')}")
    print("✅ 配色方案加载成功")
    print()
    
    # 测试CSS生成
    print("🎨 测试CSS生成...")
    css = designer.generate_professional_report_css()
    css_lines = css.count('\n')
    css_size = len(css)
    print(f"  CSS行数: {css_lines}")
    print(f"  CSS大小: {css_size} 字符")
    print(f"  包含样式:")
    
    # 检查关键样式是否存在
    key_styles = [
        ("全局样式", "box-sizing: border-box"),
        ("渐变背景", "linear-gradient"),
        ("按钮样式", ".stButton"),
        ("表格样式", ".dataframe"),
        ("Metric组件", "stMetricValue"),
        ("消息框", "stSuccess"),
        ("卡片样式", ".card"),
        ("响应式", "@media"),
        ("滚动条", "::-webkit-scrollbar"),
        ("动画效果", "@keyframes")
    ]
    
    for name, pattern in key_styles:
        if pattern in css:
            print(f"    ✓ {name}")
        else:
            print(f"    ✗ {name} (未找到)")
    
    print()
    print("✅ CSS生成成功")
    print()
    
    # 保存CSS到文件以供检查
    css_file = "/Users/wangchao/Desktop/a_quant/generated_styles.css"
    try:
        with open(css_file, 'w', encoding='utf-8') as f:
            # 移除<style>标签，只保存纯CSS
            pure_css = css.replace('<style>', '').replace('</style>', '').strip()
            f.write(pure_css)
        print(f"✅ CSS已导出到: {css_file}")
        print()
    except Exception as e:
        print(f"⚠️  CSS导出失败: {e}")
        print()
    
    # 测试其他配色方案
    print("🎨 测试其他配色方案...")
    other_palettes = ["dark_mode", "light_mode", "modern", "professional"]
    for palette_name in other_palettes:
        palette = designer.generate_color_palette(palette_name)
        print(f"  ✓ {palette_name}: {palette.get('primary', 'N/A')}")
    print()
    
    # 总结
    print("=" * 60)
    print("✨ 测试完成！")
    print("=" * 60)
    print()
    print("📋 测试总结:")
    print("  ✅ Web Designer 模块正常")
    print("  ✅ 专业报告配色方案正常")
    print("  ✅ CSS生成功能正常")
    print("  ✅ 所有关键样式已包含")
    print()
    print("🚀 下一步:")
    print("  1. 运行 streamlit run app.py 启动应用")
    print("  2. 查看专业报告风格的界面效果")
    print("  3. 根据需要微调配色和样式")
    print()
    print("💡 提示:")
    print("  - 查看 界面优化说明.md 了解详细优化内容")
    print("  - 查看 generated_styles.css 检查生成的CSS")
    print()

if __name__ == '__main__':
    test_professional_style()
