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
        'iec870ree>=0.11.3',
        'iec870ree_moxa>=0.5.1',
    ],
    description='Python wrapper for iec870ree'
)
