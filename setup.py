# The CMake interop code is based paritally on hoefling's excellent answer
# to this StackOverflow question:
# https://stackoverflow.com/questions/42585210/extending-setuptools-extension-to-use-cmake-in-setup-py

import os
import pathlib
from setuptools import setup, Extension
from setuptools.command.build_ext import build_ext as build_ext_orig

class CMakeExtension(Extension):
    def __init__(self, name):
        # don't invoke the original build_ext for this special extension
        super().__init__(name, sources=[])

class build_ext(build_ext_orig):
    def run(self):
        for ext in self.extensions:
            self.build_cmake(ext)
        super().run()

    def build_cmake(self, ext):
        cwd = pathlib.Path().absolute()

        # these dirs will be created in build_py, so if you don't have
        # any python sources to bundle, the dirs will be missing
        build_temp = pathlib.Path(self.build_temp)
        build_temp.mkdir(parents=True, exist_ok=True)
        extdir = pathlib.Path(self.get_ext_fullpath(ext.name))
        extdir.mkdir(parents=True, exist_ok=True)

        # Configure CMake arguments
        config = 'Release'
        cmake_args = [
            '-DPYLIB=' + str(extdir.parent.absolute()), # destination for the shared library
            '-DCMAKE_BUILD_TYPE=' + config
        ]
        build_args = [ '--config', config ]

        # Run CMake and build (automatically copies the .dll / .so / .dylib file)
        os.chdir(str(build_temp))
        self.spawn(['cmake', str(cwd)] + cmake_args)
        if not self.dry_run:
            self.spawn(['cmake', '--build', '.'] + build_args)
        os.chdir(str(cwd))

with open("README.md", "r") as fh:
    long_description = fh.read()

setup(
    name='SimpleImageIO',
    version='0.4.0',
    author='Pascal Grittmann',
    url='https://github.com/pgrit/SimpleImageIO',

    description='Python wrapper around TinyEXR and stb_image',
    long_description=long_description,
    long_description_content_type="text/markdown",

    packages=['simpleimageio'],
    package_dir={'simpleimageio': 'PyWrapper/simpleimageio'},
    classifiers=[
        "Programming Language :: Python :: 3",
        "License :: OSI Approved :: MIT License",
        "Operating System :: OS Independent",
    ],
    python_requires='>=3.6',
    install_requires=[
        'numpy'
    ],
    ext_modules=[CMakeExtension('simpleimageio/SimpleImageIOCore')],
    cmdclass={
        'build_ext': build_ext,
    }
)
