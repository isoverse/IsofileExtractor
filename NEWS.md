# News

## 0.3.2

- Adds Isodat readers for the `CGCInterfaceDevice` peripheral (GC II-III Interface).

## 0.3.1

- Incorporates information about corrupted files in `.imexp` archives from `isosolfs` version
  1.0 into the `issues.log`.

## 0.3.0

- `isoextract` now supports Qtegra `.imexp` notebooks on Windows, Mac OSX, and Linux, using the
  new release of `isosolfs`.
- Implements additional readers for old Isodat files.

## 0.2.1

- Fixes the handling of UTF-16 encoded `CString`s in Isodat files.

## 0.2.0

- Support added for the following file types:
  - `.bch`
  - `.iarc`
  - `.larc`
  - `.imexp` (Windows only so far)

## 0.1.2

- Updates to the Isodat file readers to capture additional peripherals and fully parse raw data
  into numeric arrays.

## 0.1.0

- Initial release of `isoextract`, supporting all common Isodat file formats:
  - `.dxf`
  - `.did`
  - `.cf`
  - `.caf`
  - `.scn`
