#include<bits/stdc++.h>
using namespace std;

int check(int a)
{
    if (a<2) return 0;
    else
    {
        for (int i=2;i<=sqrt(a);i++)
            if (a%i==0) return 0;
        return 1;
    }
}

int main()
{
    int n, i, a[1001], d=0;
    cin>>n;
    for (i=1;i<=n;i++)
    {
        cin>>a[i];
        if (check(a[i])==1)
            d++;
    }
    cout<<d;
    return 0;
}