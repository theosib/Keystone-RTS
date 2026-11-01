#!/usr/bin/env python3
"""A TCP proxy with a bad attitude, for testing what a real network does to a game.

  proxy.py LISTEN_PORT TARGET_PORT [--delay MS] [--cut-on FILE] [--kbps N]

--delay holds every byte back by this much, each way. --cut-on drops the first connection when
FILE appears, once only: later ones are left alone, so that a client can come back. --kbps limits
server-to-client bandwidth.
Standard library only, so there's no venv to set up.
"""
import argparse
import asyncio
import os


async def pump(reader, writer, delay, kbps):
    budget_time = asyncio.get_event_loop().time()
    try:
        while True:
            data = await reader.read(16384)
            if not data:
                break
            if delay:
                await asyncio.sleep(delay)
            if kbps:
                budget_time = max(budget_time, asyncio.get_event_loop().time())
                budget_time += len(data) / (kbps * 125.0)
                await asyncio.sleep(max(0.0, budget_time - asyncio.get_event_loop().time()))
            writer.write(data)
            await writer.drain()
    except (ConnectionError, asyncio.CancelledError):
        pass
    finally:
        writer.close()


async def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("listen", type=int)
    parser.add_argument("target", type=int)
    parser.add_argument("--delay", type=float, default=0.0)
    parser.add_argument("--cut-on", default="")
    parser.add_argument("--kbps", type=float, default=0.0)
    args = parser.parse_args()

    cuts = {"left": 1 if args.cut_on else 0}

    async def handle(client_reader, client_writer):
        server_reader, server_writer = await asyncio.open_connection("127.0.0.1", args.target)
        delay = args.delay / 1000.0
        tasks = [
            asyncio.create_task(pump(client_reader, server_writer, delay, 0)),
            asyncio.create_task(pump(server_reader, client_writer, delay, args.kbps)),
        ]
        if cuts["left"] > 0:
            cuts["left"] -= 1
            while not os.path.exists(args.cut_on):
                await asyncio.sleep(0.02)
            for task in tasks:
                task.cancel()
            client_writer.transport.abort()
            server_writer.transport.abort()
            print("proxy: cut a connection", flush=True)
        await asyncio.gather(*tasks, return_exceptions=True)

    server = await asyncio.start_server(handle, "127.0.0.1", args.listen)
    async with server:
        await server.serve_forever()


asyncio.run(main())
