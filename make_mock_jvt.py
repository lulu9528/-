import dill

class _MockOutput:
    def __init__(self, img):
        self._img = img
    def value(self):
        return self._img

class MockCameraTool:
    """仿真相机工具：无硬件，run() 时实时生成一张带特征圆点的灰度图（模拟相机帧）。
    接口与 JVS CameraTool 对齐：run() / get_output('output_image').value() -> pyjvl2 Image。
    引擎/numpy 在函数内延迟导入：保证 dill recurse 序列化自包含（不递归引擎模块）。"""
    def __init__(self):
        self._w = 640
        self._h = 480
        self._frame = 0

    def _make_img(self):
        import numpy as np
        from pyjvl2._C.libjvl import Image
        self._frame += 1
        arr = np.full((self._h, self._w), 200, dtype=np.uint8)
        cx = self._w // 2 + (self._frame % 3) * 3 - 3
        cy = self._h // 2
        yy, xx = np.ogrid[:self._h, :self._w]
        arr[(xx - cx) ** 2 + (yy - cy) ** 2 <= 40 ** 2] = 30
        arr[((xx - cx - 80) ** 2 + (yy - cy + 60) ** 2) <= 15 ** 2] = 30
        arr[((xx - cx + 80) ** 2 + (yy - cy - 60) ** 2) <= 15 ** 2] = 30
        return Image(arr)

    def run(self):
        self._img = self._make_img()

    def get_output(self, name):
        if name == 'output_image':
            return _MockOutput(self._img)
        raise KeyError('unknown pin: ' + str(name))

tool = MockCameraTool()
out = r'C:\Users\Administrator\AppData\Local\Doubao\User Data\Profile 2\.doubao\agent_mode\workspace\.sessions\38447195940231170\agents\m_0cwRbmFfoEf\相机取图_仿真.jvt'
with open(out, 'wb') as f:
    dill.dump(tool, f, recurse=True)
print('JVT_WRITTEN', out)
