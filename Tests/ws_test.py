import asyncio
import websockets

async def test_ws():
    uri = "ws://localhost:5114/api/room/ws"  # change port if needed

    async with websockets.connect(uri) as websocket:
        print("Connected to server")

        # Send message
        message = "Hello from Python 👋"
        await websocket.send(message)
        print(f"Sent: {message}")

        # Receive echo
        response = await websocket.recv()
        print(f"Received: {response}")

        # Send another message
        await websocket.send("Second message")
        print("Sent second message")

        response = await websocket.recv()
        print(f"Received: {response}")

asyncio.run(test_ws())