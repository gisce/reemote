from setuptools import setup, find_packages
from pyreemote import __LIBRARY_VERSION__

setup(
    name='pyreemote',
    version=__LIBRARY_VERSION__,
    packages=find_packages(),
    url='https://github.com/gisce/reemote',
    license='GNU Affero General Public License v3',
    author='GISCE-TI, S.L.',
    author_email='devel@gisce.net',
    install_requires=[
        'requests',
        'iec870ree>=0.14.1',
        'iec870ree-moxa>=0.5.2',
    ],
    description='Python wrapper for iec870ree'
)
