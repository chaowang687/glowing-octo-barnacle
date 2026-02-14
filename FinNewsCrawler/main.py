#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
财经新闻爬虫 - 主入口文件
命令行工具，支持搜索新闻、资金流向、行业板块等数据
"""

import argparse
import sys
from datetime import datetime
from tabulate import tabulate

from modules import (
    StorageManager,
    NewsCrawler,
    FundCrawler,
    SectorCrawler,
    crawl_stock_full_data,
    crawl_market_overview
)
from utils import logger


def print_header(title: str):
    """打印标题"""
    print("\n" + "=" * 60)
    print(f"  {title}")
    print("=" * 60)


def print_news(news_list: list):
    """打印新闻列表"""
    if not news_list:
        print("未找到相关新闻")
        return
    
    table_data = []
    for news in news_list[:20]:  # 限制显示前20条
        table_data.append([
            news.get('pub_date', '')[:10],
            news.get('title', '')[:50],
            news.get('stock_name', news.get('stock_code', ''))
        ])
    
    print(tabulate(table_data, 
                   headers=['日期', '标题', '股票'], 
                   tablefmt='grid',
                   maxcolwidths=[12, 50, 15]))


def print_funds(funds_list: list):
    """打印资金流向"""
    if not funds_list:
        print("未找到资金流向数据")
        return
    
    table_data = []
    for fund in funds_list[:15]:
        main_inflow = fund.get('main_net_inflow', 0)
        table_data.append([
            fund.get('trade_date', ''),
            f"{main_inflow/10000:.2f}万" if main_inflow else "N/A",
            f"{fund.get('main_net_inflow_ratio', 0):.2f}%",
        ])
    
    print(tabulate(table_data,
                   headers=['日期', '主力净流入', '主力占比'],
                   tablefmt='grid'))


def print_sectors(sectors_list: list, title: str = "行业板块"):
    """打印行业板块"""
    if not sectors_list:
        print(f"未找到{title}数据")
        return
    
    table_data = []
    for sector in sectors_list[:15]:
        net_inflow = sector.get('net_inflow', 0)
        change = sector.get('change_percent', 0)
        table_data.append([
            sector.get('sector_name', '')[:15],
            f"{net_inflow/10000:.2f}万" if net_inflow else "0",
            f"{change:+.2f}%",
            f"{sector.get('turnover_rate', 0):.2f}%"
        ])
    
    print(tabulate(table_data,
                   headers=['板块名称', '净流入(万)', '涨跌幅', '换手率'],
                   tablefmt='grid'))


def cmd_search_news(args):
    """搜索新闻命令"""
    print_header(f"新闻搜索: {args.keyword}")
    
    crawler = NewsCrawler()
    news_list = crawler.search_news(args.keyword, days=args.days)
    crawler.close()
    
    print_news(news_list)
    
    # 保存到数据库
    if args.save and news_list:
        with StorageManager() as db:
            db.save_news(news_list)
        print("\n✅ 数据已保存到数据库")


def cmd_stock_analysis(args):
    """股票分析命令"""
    print_header(f"股票分析: {args.code}")
    
    # 爬取完整数据
    result = crawl_stock_full_data(
        args.code, 
        args.name or "", 
        days=args.days
    )
    
    # 打印新闻
    print("\n【相关新闻】")
    print_news(result.get('news', []))
    
    # 打印资金流向
    print("\n【主力资金流向】")
    print_funds(result.get('funds', []))
    
    # 打印事件
    events = result.get('events', [])
    if events:
        print("\n【相关事件】")
        for event in events[:10]:
            print(f"  [{event.get('event_type')}] {event.get('event_title', '')[:50]}")
    
    # 保存到数据库
    if args.save:
        with StorageManager() as db:
            db.save_news(result.get('news', []))
            db.save_funds(result.get('funds', []))
            db.save_events(result.get('events', []))
        print("\n✅ 数据已保存到数据库")


def cmd_market_overview(args):
    """市场概览命令"""
    print_header("市场概览")
    
    result = crawl_market_overview(days=args.days)
    
    # 打印热门板块
    print("\n【资金净流入板块 TOP10】")
    print_sectors(result.get('hot_sectors', []), "热门板块")
    
    # 打印冷门板块
    print("\n【资金净流出板块 TOP10】")
    print_sectors(result.get('cold_sectors', []), "冷门板块")
    
    # 保存到数据库
    if args.save:
        with StorageManager() as db:
            all_sectors = result.get('hot_sectors', []) + result.get('cold_sectors', [])
            db.save_sectors(all_sectors)
        print("\n✅ 数据已保存到数据库")


def cmd_export(args):
    """导出数据命令"""
    from modules.storage import save_to_csv
    
    print_header("数据导出")
    
    with StorageManager() as db:
        if args.type == "news":
            df = db.get_news(days=args.days, limit=args.limit)
            if not df.empty:
                filepath = save_to_csv(df, f"news_{datetime.now().strftime('%Y%m%d')}.csv")
                print(f"\n✅ 已导出 {len(df)} 条新闻到: {filepath}")
            else:
                print("没有找到新闻数据")
                
        elif args.type == "funds":
            if not args.code:
                print("导出资金数据需要指定股票代码: --code")
                return
            df = db.get_funds(args.code, days=args.days)
            if not df.empty:
                filepath = save_to_csv(df, f"funds_{args.code}_{datetime.now().strftime('%Y%m%d')}.csv")
                print(f"\n✅ 已导出 {len(df)} 条资金数据到: {filepath}")
            else:
                print("没有找到资金数据")
                
        elif args.type == "sectors":
            df = db.get_sectors(limit=args.limit)
            if not df.empty:
                filepath = save_to_csv(df, f"sectors_{datetime.now().strftime('%Y%m%d')}.csv")
                print(f"\n✅ 已导出 {len(df)} 条行业数据到: {filepath}")
            else:
                print("没有找到行业数据")


def main():
    """主函数"""
    parser = argparse.ArgumentParser(
        description="财经新闻爬虫 - 获取新闻、资金流向、行业动态",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
使用示例:
  # 搜索关键词新闻
  python main.py search -k "半导体" --days 7
  
  # 分析单只股票
  python main.py analyze -c 600519 -n 茅台
  
  # 查看市场概览
  python main.py market
  
  # 导出数据
  python main.py export -t news --days 30
        """
    )
    
    subparsers = parser.add_subparsers(dest="command", help="子命令")
    
    # 新闻搜索命令
    search_parser = subparsers.add_parser("search", help="搜索财经新闻")
    search_parser.add_argument("-k", "--keyword", required=True, help="搜索关键词")
    search_parser.add_argument("--days", type=int, default=7, help="查询天数(默认7天)")
    search_parser.add_argument("--save", action="store_true", help="保存到数据库")
    
    # 股票分析命令
    analyze_parser = subparsers.add_parser("analyze", help="分析单只股票")
    analyze_parser.add_argument("-c", "--code", required=True, help="股票代码")
    analyze_parser.add_argument("-n", "--name", help="股票名称")
    analyze_parser.add_argument("--days", type=int, default=7, help="查询天数(默认7天)")
    analyze_parser.add_argument("--save", action="store_true", help="保存到数据库")
    
    # 市场概览命令
    market_parser = subparsers.add_parser("market", help="查看市场概览")
    market_parser.add_argument("--days", type=int, default=1, help="查询天数(默认1天)")
    market_parser.add_argument("--save", action="store_true", help="保存到数据库")
    
    # 导出命令
    export_parser = subparsers.add_parser("export", help="导出数据")
    export_parser.add_argument("-t", "--type", choices=["news", "funds", "sectors"], 
                              required=True, help="数据类型")
    export_parser.add_argument("--days", type=int, default=30, help="查询天数(默认30天)")
    export_parser.add_argument("--limit", type=int, default=100, help="返回数量限制")
    export_parser.add_argument("-c", "--code", help="股票代码(导出资金数据时必填)")
    
    args = parser.parse_args()
    
    if not args.command:
        parser.print_help()
        return
    
    try:
        if args.command == "search":
            cmd_search_news(args)
        elif args.command == "analyze":
            cmd_stock_analysis(args)
        elif args.command == "market":
            cmd_market_overview(args)
        elif args.command == "export":
            cmd_export(args)
    except KeyboardInterrupt:
        print("\n\n操作已取消")
        sys.exit(0)
    except Exception as e:
        logger.error(f"执行出错: {e}")
        sys.exit(1)


if __name__ == "__main__":
    main()
