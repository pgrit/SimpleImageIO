import simpleimageio as sio

input = sio.read("memorial.pfm")
reinhard = sio.reinhard(input, 4)
aces = sio.aces(input)

sio.write("reinhard.exr", reinhard)
sio.write("aces.exr", aces)

with sio.TevIpc() as tev:
    tev.display_image("reinhard.exr", reinhard)
    tev.display_layered_image("tonemapped", {"reinhard": reinhard, "aces": aces})