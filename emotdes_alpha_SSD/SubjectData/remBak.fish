for i in $argv
  set fns
  set dups
  for fn in $i*.bak
    set nwfn (echo $fn | sed 's/_[0-9]*\.bak//g')
      if not contains $nwfn $fns
        echo "renamed $nwfn from $fn"
        mv $fn $nwfn
        set fns $fns $nwfn
      else
        echo -e "\n$fn kept unchanged\n"
        set dups $dups $nwfn
      end
  end
#   echo (count $fns)
  set cnt (count $fns)
  set dcnt (count $dups)
  echo -e "\n$cnt files renamed"
  echo -e "\n$dcnt duplicates remaining\n"
#   echo $fns[$cnt]
#   echo $fns[1]
  for n in $dups
    echo $n
  end
end