@echo off

echo %1
pushd %1

if not exist Logs goto Done

cd Logs

for /D %%d in (*) do (
	echo ... Delete files from %%d
    cd %%d

    del Progrunner_??-02-????.txt
    del Progrunner_??-03-????.txt
    del Progrunner_??-04-????.txt
    del Progrunner_??-05-????.txt
    del Progrunner_??-06-????.txt
    del Progrunner_??-07-????.txt
    del Progrunner_??-08-????.txt
    del Progrunner_??-09-????.txt
    del Progrunner_??-1?-????.txt
    del Progrunner_??-2?-????.txt
    del Progrunner_??-3?-????.txt

    cd ..
)

rem Delete log files for the current year, months 1 through 9
FOR /L %%G IN (1,1,9) DO (
    del Progrunner_0%%G-02-????.txt
    del Progrunner_0%%G-03-????.txt
    del Progrunner_0%%G-04-????.txt
    del Progrunner_0%%G-05-????.txt
    del Progrunner_0%%G-06-????.txt
    del Progrunner_0%%G-07-????.txt
    del Progrunner_0%%G-08-????.txt
    del Progrunner_0%%G-09-????.txt
    del Progrunner_0%%G-1?-????.txt
    del Progrunner_0%%G-2?-????.txt
    del Progrunner_0%%G-3?-????.txt
)

popd

:Done
