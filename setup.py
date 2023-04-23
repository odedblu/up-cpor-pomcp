#!/usr/bin/env python3

from setuptools import setup, find_packages
import up_cpor


long_description=\
'''
 ============================================================
    UP_CPOR
 ============================================================
    up_cpor is a small package that allows an exchange of
    equivalent data structures between unified_planning and CPOR.
    It automatically considers the different programming languages.
'''

setup(name='up_cpor',
      version=up_cpor.__version__,
      include_package_data=True,
      description='up_cpor',
      author='BGU CPOR Development Team',
      author_email='shanigu@bgu.ac.il',
      url='',
      packages= find_packages(),
      install_requires=['pythonnet==3.0.0'],
      python_requires='>=3.7',
      package_data={"CPORLib": ["**/*.dll", "**/*.exe"]},
      license='APACHE'
)