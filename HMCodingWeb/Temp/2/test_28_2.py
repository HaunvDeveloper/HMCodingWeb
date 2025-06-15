#include <iostream>
using namespace std;

int main() {
    int N;
    cin >> N;

    // Số hoàn hảo phải là số dương lớn hơn 1
    if (N <= 1) {
        cout << "NO" << endl;
        return 0;
    }

    int sum = 0;
    // Duyệt tất cả các ước số từ 1 đến N/2
    for (int i = 1; i <= N / 2; ++i) {
        if (N % i == 0) {
            sum += i;
        }
    }

    if (sum == N)
        cout << "YES" << endl;
    else
        cout << "NO" << endl;

    return 0;
}
